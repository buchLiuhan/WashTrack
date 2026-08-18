using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using WashTrack.Data;
using WashTrack.Models;

namespace WashTrack.MVVM.ViewModels
{
    // One row of the inventory report: how much of this item was consumed
    // in the selected date range, alongside its current live stock.
    public partial class InventoryUsageReportRow : ObservableObject
    {
        public string ItemName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal QuantityUsed { get; set; }
        public decimal CurrentStock { get; set; }
        public bool IsLowStock { get; set; }

        public string UsedText => $"Used: {QuantityUsed:F1}{Unit}";
        public string StockText => $"{CurrentStock:F0}{Unit}";
    }

    public partial class ReportsViewModel : ObservableObject
    {
        private readonly WashTrackContext _context;

        [ObservableProperty]
        private ObservableCollection<Transaction> transactions = new();

        [ObservableProperty]
        private decimal totalRevenue;

        [ObservableProperty]
        private int totalTransactions;

        [ObservableProperty]
        private decimal averageTransactionValue;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private DateTime startDate = DateTime.Today.AddDays(-30);

        [ObservableProperty]
        private DateTime endDate = DateTime.Today;

        // Sales/Inventory segmented toggle
        [ObservableProperty]
        private bool showingInventoryReport = false;

        [ObservableProperty]
        private string reportToggleText = "View Inventory Report";

        [ObservableProperty]
        private ObservableCollection<InventoryUsageReportRow> inventoryUsage = new();

        [ObservableProperty]
        private int lowStockCount;

        public ReportsViewModel(WashTrackContext context)
        {
            _context = context;
        }

        [RelayCommand]
        public async Task LoadReportsAsync()
        {
            IsLoading = true;

            var list = await _context.Transactions
               .Include(t => t.Customer)
               .Include(t => t.Items)
               .ThenInclude(i => i.Service)
               .Where(t => t.CreatedAt.Date >= StartDate.Date &&
                t.CreatedAt.Date <= EndDate.Date)
               .OrderByDescending(t => t.CreatedAt)
               .ToListAsync();

            Transactions = new ObservableCollection<Transaction>(list);
            TotalTransactions = list.Count;
            TotalRevenue = list
                .Where(t => t.Status == "Completed")
                .Sum(t => t.TotalCost);
            AverageTransactionValue = TotalTransactions > 0
                ? TotalRevenue / TotalTransactions
                : 0;

            await LoadInventoryReportAsync();

            IsLoading = false;
        }

        // Consumption per item within the same date range, alongside a live
        // stock snapshot (stock itself isn't date-ranged — it's "now").
        private async Task LoadInventoryReportAsync()
        {
            var activeItems = await _context.Inventories
                .AsNoTracking()
                .Where(i => i.IsActive)
                .OrderBy(i => i.ItemName)
                .ToListAsync();

            var usageTotals = await _context.InventoryUsageHistories
                .AsNoTracking()
                .Where(h => h.UsageDate.Date >= StartDate.Date && h.UsageDate.Date <= EndDate.Date)
                .GroupBy(h => h.InventoryId)
                .Select(g => new { InventoryId = g.Key, Total = g.Sum(h => h.QuantityUsed) })
                .ToListAsync();

            var rows = activeItems
                .Select(i => new InventoryUsageReportRow
                {
                    ItemName = i.ItemName,
                    Unit = i.Unit,
                    QuantityUsed = usageTotals.FirstOrDefault(u => u.InventoryId == i.InventoryId)?.Total ?? 0m,
                    CurrentStock = i.CurrentStock,
                    IsLowStock = i.IsLowStock
                })
                .OrderByDescending(r => r.QuantityUsed)
                .ToList();

            InventoryUsage = new ObservableCollection<InventoryUsageReportRow>(rows);
            LowStockCount = activeItems.Count(i => i.IsLowStock);
        }

        [RelayCommand]
        public async Task FilterAsync()
        {
            await LoadReportsAsync();
        }

        // One button, two states: switches the report body between Sales
        // and Inventory. Both share the same date range and data already
        // loaded, so this never needs to re-query.
        [RelayCommand]
        public void ToggleReportView()
        {
            ShowingInventoryReport = !ShowingInventoryReport;
            ReportToggleText = ShowingInventoryReport ? "View Sales Report" : "View Inventory Report";
        }
    }
}