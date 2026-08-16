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

        // Average consumed per day over the last 30 days. Zero means the
        // item has never been used yet.
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

        public DateTime ReorderDate => DateTime.Today.AddDays(DaysUntilThreshold);

        // What the card actually shows under the item name.
        public string ForecastText
        {
            get
            {
                if (!HasUsageData)
                    return "No usage data yet";

                if (Item.IsLowStock)
                    return "Restock now";

                return $"~{DaysUntilThreshold} days left · reorder by {ReorderDate:MMM dd}";
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

            // Single query for all usage in the window, grouped by item.
            var windowStart = DateTime.Today.AddDays(-30);
            var usageTotals = await _context.InventoryUsageHistories
                .AsNoTracking()
                .Where(h => h.UsageDate >= windowStart)
                .GroupBy(h => h.InventoryId)
                .Select(g => new { InventoryId = g.Key, Total = g.Sum(h => h.QuantityUsed) })
                .ToListAsync();

            var wrapped = items.Select(i =>
            {
                var total = usageTotals.FirstOrDefault(u => u.InventoryId == i.InventoryId)?.Total ?? 0m;
                return new InventoryWithUsage
                {
                    Item = i,
                    AverageDailyUsage = total / 30m
                };
            }).ToList();

            InventoryItems = new ObservableCollection<InventoryWithUsage>(wrapped);

            var lowStock = items.Where(i => i.IsLowStock).ToList();
            HasLowStock = lowStock.Count > 0 && !ShowingInactive;
            TotalItems = items.Count;
            LowStockCount = lowStock.Count;

            IsLoading = false;
        }

        // Switches between the active and inactive lists.
        [RelayCommand]
        public async Task ToggleInactiveAsync()
        {
            ShowingInactive = !ShowingInactive;
            ToggleButtonText = ShowingInactive ? "Show Active" : "Show Inactive";
            await LoadInventoryAsync();
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