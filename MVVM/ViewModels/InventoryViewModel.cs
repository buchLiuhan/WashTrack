using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using WashTrack.Data;
using WashTrack.Models;
using WashTrack.MVVM.Views;

namespace WashTrack.MVVM.ViewModels
{
    public partial class InventoryViewModel : ObservableObject
    {
        private readonly WashTrackContext _context;

        [ObservableProperty]
        private ObservableCollection<Inventory> inventoryItems = new();

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

        // Loads active or inactive items depending on the toggle.
        // AsNoTracking keeps EF from handing back stale cached objects,
        // which would leave the card values frozen after an edit.
        [RelayCommand]
        public async Task LoadInventoryAsync()
        {
            IsLoading = true;

            var items = await _context.Inventories
                .AsNoTracking()
                .Where(i => i.IsActive != ShowingInactive)
                .OrderBy(i => i.ItemName)
                .ToListAsync();

            InventoryItems = new ObservableCollection<Inventory>(items);

            // Low stock stats only make sense for items still in use.
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
        public async Task EditItemAsync(Inventory item)
        {
            var parameters = new Dictionary<string, object>
            {
                { "InventoryItem", item }
            };
            await Shell.Current.GoToAsync(nameof(InventoryDetailPage), parameters);
        }

        // ===== DEACTIVATE / RESTORE =====

        // Soft delete. Blocked while an active service still depends on this
        // item, otherwise that service would silently stop deducting stock.
        [RelayCommand]
        public async Task DeactivateItemAsync(Inventory item)
        {
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
        public async Task RestoreItemAsync(Inventory item)
        {
            var tracked = await _context.Inventories.FindAsync(item.InventoryId);
            if (tracked == null) return;

            tracked.IsActive = true;
            tracked.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            await LoadInventoryAsync();
        }

        // ===== PERMANENT DELETE =====

        // Only allowed when nothing depends on this item. Usage history
        // feeds inventory reports, so an item with history can never be
        // permanently removed without corrupting past figures.
        [RelayCommand]
        public async Task PermanentDeleteItemAsync(Inventory item)
        {
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