using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using WashTrack.Data;
using WashTrack.Models;
using WashTrack.MVVM.Views;

namespace WashTrack.MVVM.ViewModels
{
    // Display wrapper: pairs an inventory item with its usage forecast.
    // The forecast can't live on the model itself because it depends on
    // a separate table (InventoryUsageHistory).
    public partial class InventoryWithUsage : ObservableObject
    {
        public Inventory Item { get; set; } = new();

        // "5000 grams" on one line instead of stacked number/unit rows —
        // keeps the card compact and stops it looking disjointed.
        public string StockText => $"{Item.CurrentStock:F0} {Item.Unit}";

        // Weighted average consumed per day, recent days counting for more.
        // See CalculateWeightedDailyUsage. Zero means the item has never
        // been used yet.
        public decimal AverageDailyUsage { get; set; }

        public bool HasUsageData => AverageDailyUsage > 0;

        // Days until stock reaches the owner's own low-stock threshold —
        // not zero. That way the forecast and the Low Stock pill agree,
        // and she gets warning time for the 1-day supply lead time.
        public int DaysUntilThreshold
        {
            get
            {
                if (!HasUsageData) return 0;
                decimal usable = Item.CurrentStock - Item.MinimumThreshold;
                if (usable <= 0) return 0;
                return (int)(usable / AverageDailyUsage);
            }
        }

        public DateTime RestockByDate => DateTime.Today.AddDays(DaysUntilThreshold);

        // What the card actually shows under the item name.
        public string ForecastText
        {
            get
            {
                if (!HasUsageData)
                    return "No usage data yet";

                // Once it's low, "how many days left" is no longer the useful
                // number — how much to add is. Echo back the owner's own usual
                // restock amount if she set one, labelled as hers rather than
                // as advice the app worked out.
                if (Item.IsLowStock)
                    return Item.UsualRestockAmount.HasValue
                        ? $"Restock now · usually {Item.UsualRestockAmount.Value:F0} {Item.Unit}"
                        : "Restock now";

                // Include the year once the date leaves the current one:
                // "MMM dd" alone rendered a 2027 restock date as "Aug 27",
                // which reads as days away instead of a year away.
                string dateText = RestockByDate.Year == DateTime.Today.Year
                    ? $"{RestockByDate:MMM dd}"
                    : $"{RestockByDate:MMM dd, yyyy}";

                return $"~{DaysUntilThreshold} days left · restock by {dateText}";
            }
        }
    }

    public partial class InventoryViewModel : ObservableObject
    {
        private readonly WashTrackContext _context;

        [ObservableProperty]
        private ObservableCollection<InventoryWithUsage> inventoryItems = new();

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private bool hasLowStock;

        [ObservableProperty]
        private int totalItems;

        [ObservableProperty]
        private int lowStockCount;

        [ObservableProperty]
        private bool showingInactive = false;

        [ObservableProperty]
        private string toggleButtonText = "Show Inactive";

        [ObservableProperty]
        private bool showingLowStockOnly = false;

        [ObservableProperty]
        private string lowStockToggleText = "⚠ View Low Stock";

        [ObservableProperty]
        private string lowStockBannerText = string.Empty;

        // Full active/inactive list from the last load, before the low-stock
        // filter is applied. Lets the toggle re-filter in memory instead of
        // hitting the database again.
        private List<InventoryWithUsage> _loadedItems = new();

        public InventoryViewModel(WashTrackContext context)
        {
            _context = context;
        }

        // ===== LOADING =====

        // Loads items plus their 30-day usage totals in two queries,
        // then pairs them in memory. Avoids one query per item.
        [RelayCommand]
        public async Task LoadInventoryAsync()
        {
            IsLoading = true;

            var items = await _context.Inventories
                .AsNoTracking()
                .Where(i => i.IsActive != ShowingInactive)
                .OrderBy(i => i.ItemName)
                .ToListAsync();

            // Single query for all usage in the window. Rows come back
            // individually rather than pre-grouped because the weighted
            // average needs to know which day each one landed on.
            var windowStart = DateTime.Today.AddDays(-(UsageWindowDays - 1));
            var usageRows = await _context.InventoryUsageHistories
                .AsNoTracking()
                .Where(h => h.UsageDate >= windowStart)
                .Select(h => new { h.InventoryId, h.UsageDate, h.QuantityUsed })
                .ToListAsync();

            var usageByItem = usageRows
                .GroupBy(h => h.InventoryId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(h => (Date: h.UsageDate, Quantity: h.QuantityUsed)).ToList());

            _loadedItems = items.Select(i =>
            {
                usageByItem.TryGetValue(i.InventoryId, out var rows);
                return new InventoryWithUsage
                {
                    Item = i,
                    AverageDailyUsage = CalculateWeightedDailyUsage(rows)
                };
            }).ToList();

            ApplyLowStockFilter();

            var lowStock = items.Where(i => i.IsLowStock).ToList();
            HasLowStock = lowStock.Count > 0 && !ShowingInactive;
            TotalItems = items.Count;
            LowStockCount = lowStock.Count;
            LowStockBannerText = BuildLowStockBannerText(lowStock);

            IsLoading = false;
        }

        // ===== USAGE FORECASTING =====

        // Longest stretch of history the forecast will look back over.
        private const int UsageWindowDays = 30;

        // Weighted Moving Average of daily consumption.
        //
        //     D = Σ(wᵢ · xᵢ) / Σ(wᵢ)
        //
        // where xᵢ is the quantity used on day i and the weights wᵢ rise
        // linearly toward the present. Two things this gets right that a
        // flat total/30 did not:
        //
        //   1. The divisor is how long the item has actually been in use,
        //      not a fixed 30. An item first used three days ago used to
        //      have its consumption spread over a month it hadn't lived
        //      through, which reported a tenth of the real daily figure and
        //      promised the owner ten times the days of stock she had.
        //
        //   2. Recent days count for more. A flat mean treats consumption
        //      from four weeks ago as evidence about tomorrow; this doesn't.
        private static decimal CalculateWeightedDailyUsage(
            List<(DateTime Date, decimal Quantity)>? usage)
        {
            if (usage == null || usage.Count == 0) return 0m;

            var today = DateTime.Today;
            var firstUsageDay = usage.Min(u => u.Date).Date;

            // Observation window: first recorded usage through today,
            // inclusive, capped at UsageWindowDays.
            int days = (today - firstUsageDay).Days + 1;
            days = Math.Clamp(days, 1, UsageWindowDays);

            var windowStart = today.AddDays(-(days - 1));

            // Bucket into calendar days. Days with no usage stay at zero on
            // purpose — stock has to last through closed days too, so they
            // belong in a "how many days will this last" figure.
            var dailyTotals = new decimal[days];
            foreach (var (date, quantity) in usage)
            {
                int index = (date.Date - windowStart).Days;
                if (index >= 0 && index < days)
                    dailyTotals[index] += quantity;
            }

            // Oldest day in the window weighs 1, newest weighs `days`.
            decimal weightedSum = 0m;
            decimal weightTotal = 0m;
            for (int i = 0; i < days; i++)
            {
                decimal weight = i + 1;
                weightedSum += weight * dailyTotals[i];
                weightTotal += weight;
            }

            return weightTotal > 0 ? weightedSum / weightTotal : 0m;
        }

        // Re-slices the already-loaded list instead of re-querying —
        // the toggle just changes what's shown, not what's loaded.
        private void ApplyLowStockFilter()
        {
            var source = ShowingLowStockOnly
                ? _loadedItems.Where(w => w.Item.IsLowStock)
                : _loadedItems;
            InventoryItems = new ObservableCollection<InventoryWithUsage>(source);
        }

        // Names the single most urgent item so the banner isn't just a
        // generic notice. "Most urgent" = furthest past its own threshold
        // (CurrentStock - MinimumThreshold, most negative wins) since raw
        // stock numbers aren't comparable across items with different units.
        private static string BuildLowStockBannerText(List<Inventory> lowStock)
        {
            if (lowStock.Count == 0) return string.Empty;

            var worst = lowStock
                .OrderBy(i => i.CurrentStock - i.MinimumThreshold)
                .First();

            string suffix = lowStock.Count > 1 ? $" (+{lowStock.Count - 1} more)" : string.Empty;
            // No emoji here — the banner already renders a warning icon
            // beside this text, and the two together read as a stutter.
            return $"{worst.ItemName} is critically low — {worst.CurrentStock:F0}{worst.Unit} left (min {worst.MinimumThreshold:F0}{worst.Unit}).{suffix}";
        }

        // Switches between the active and inactive lists.
        [RelayCommand]
        public async Task ToggleInactiveAsync()
        {
            ShowingInactive = !ShowingInactive;
            ToggleButtonText = ShowingInactive ? "Show Active" : "Show Inactive";

            // Low-stock filter only makes sense on the active list.
            ShowingLowStockOnly = false;
            LowStockToggleText = "⚠ View Low Stock";

            await LoadInventoryAsync();
        }

        // One button, two states: switches the active list between the
        // full alphabetical view and low-stock-only, without a reload.
        [RelayCommand]
        public void ToggleLowStockOnly()
        {
            ShowingLowStockOnly = !ShowingLowStockOnly;
            LowStockToggleText = ShowingLowStockOnly ? "View All Items" : "⚠ View Low Stock";
            ApplyLowStockFilter();
        }

        // ===== NAVIGATION =====

        [RelayCommand]
        public async Task AddItemAsync()
        {
            await Shell.Current.GoToAsync(nameof(InventoryDetailPage));
        }

        [RelayCommand]
        public async Task EditItemAsync(InventoryWithUsage row)
        {
            var parameters = new Dictionary<string, object>
            {
                { "InventoryItem", row.Item }
            };
            await Shell.Current.GoToAsync(nameof(InventoryDetailPage), parameters);
        }

        [RelayCommand]
        public async Task RestockItemAsync(InventoryWithUsage row)
        {
            var parameters = new Dictionary<string, object>
            {
                { "InventoryItem", row.Item }
            };
            await Shell.Current.GoToAsync(nameof(InventoryRestockPage), parameters);
        }

        // ===== DEACTIVATE / RESTORE =====

        // Soft delete. Blocked while an active service still depends on this
        // item, otherwise that service would silently stop deducting stock.
        [RelayCommand]
        public async Task DeactivateItemAsync(InventoryWithUsage row)
        {
            var item = row.Item;

            var linkedService = await _context.Services
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.IsActive &&
                    (s.DetergentItemId == item.InventoryId ||
                     s.ConditionerItemId == item.InventoryId ||
                     s.OtherItemId == item.InventoryId));

            if (linkedService != null)
            {
                await Shell.Current.DisplayAlert(
                    "Cannot Deactivate",
                    $"'{item.ItemName}' is still used by the service '{linkedService.ServiceName}'. Update or deactivate that service first.",
                    "OK");
                return;
            }

            bool confirm = await Shell.Current.DisplayAlert(
                "Deactivate Item",
                $"Move '{item.ItemName}' to inactive? Its usage history will be preserved.",
                "Yes", "No");

            if (!confirm) return;

            var tracked = await _context.Inventories.FindAsync(item.InventoryId);
            if (tracked == null) return;

            tracked.IsActive = false;
            tracked.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            await LoadInventoryAsync();
        }

        [RelayCommand]
        public async Task RestoreItemAsync(InventoryWithUsage row)
        {
            var tracked = await _context.Inventories.FindAsync(row.Item.InventoryId);
            if (tracked == null) return;

            tracked.IsActive = true;
            tracked.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            await LoadInventoryAsync();
        }

        // ===== PERMANENT DELETE =====

        // Only allowed when nothing depends on this item. Usage history
        // feeds inventory reports, so an item with history can never be
        // permanently removed without changing past figures.
        [RelayCommand]
        public async Task PermanentDeleteItemAsync(InventoryWithUsage row)
        {
            var item = row.Item;

            var hasHistory = await _context.InventoryUsageHistories
                .AsNoTracking()
                .AnyAsync(h => h.InventoryId == item.InventoryId);

            if (hasHistory)
            {
                await Shell.Current.DisplayAlert(
                    "Cannot Delete",
                    $"'{item.ItemName}' has recorded usage history and cannot be permanently deleted, since that would change past inventory reports. It will stay hidden as inactive instead.",
                    "OK");
                return;
            }

            bool confirm = await Shell.Current.DisplayAlert(
                "Permanently Delete",
                $"Permanently delete '{item.ItemName}'? This cannot be undone.",
                "Delete Forever", "Cancel");

            if (!confirm) return;

            var tracked = await _context.Inventories.FindAsync(item.InventoryId);
            if (tracked == null) return;

            _context.Inventories.Remove(tracked);
            await _context.SaveChangesAsync();
            await LoadInventoryAsync();
        }
    }
}