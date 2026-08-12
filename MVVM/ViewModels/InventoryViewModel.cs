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
        private ObservableCollection<Inventory> lowStockItems = new();

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private bool hasLowStock;

        [ObservableProperty]
        private int totalItems;

        [ObservableProperty]
        private int lowStockCount;

        public InventoryViewModel(WashTrackContext context)
        {
            _context = context;
        }

        [RelayCommand]
        public async Task LoadInventoryAsync()
        {
            IsLoading = true;

            var items = await _context.Inventories
                .OrderBy(i => i.ItemName)
                .ToListAsync();

            InventoryItems = new ObservableCollection<Inventory>(items);

            var lowStock = items
                .Where(i => i.CurrentStock <= i.MinimumThreshold)
                .ToList();

            LowStockItems = new ObservableCollection<Inventory>(lowStock);
            HasLowStock = lowStock.Count > 0;
            TotalItems = items.Count;
            LowStockCount = lowStock.Count;

            IsLoading = false;
        }

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

        [RelayCommand]
        public async Task DeleteItemAsync(Inventory item)
        {
            bool confirm = await Shell.Current.DisplayAlert(
                "Delete Item",
                $"Are you sure you want to delete '{item.ItemName}'?",
                "Yes", "No");

            if (!confirm) return;

            _context.Inventories.Remove(item);
            await _context.SaveChangesAsync();
            await LoadInventoryAsync();
        }

        // Estimate days remaining based on usage history
        public async Task<int> EstimateDaysRemainingAsync(int inventoryId, decimal currentStock)
        {
            var thirtyDaysAgo = DateTime.Now.AddDays(-30);

            var totalUsed = await _context.InventoryUsageHistories
                .Where(h => h.InventoryId == inventoryId && h.UsageDate >= thirtyDaysAgo)
                .SumAsync(h => h.QuantityUsed);

            if (totalUsed == 0) return 999; // No usage data = essentially infinite

            decimal avgDailyUsage = totalUsed / 30m;
            return (int)(currentStock / avgDailyUsage);
        }
    }
}