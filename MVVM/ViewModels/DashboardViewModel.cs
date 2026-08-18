using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using WashTrack.Data;
using WashTrack.Models;
using WashTrack.MVVM.Views;

namespace WashTrack.MVVM.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly WashTrackContext _context;

        [ObservableProperty]
        private decimal totalSalesToday;

        [ObservableProperty]
        private decimal totalSalesThisMonth;

        [ObservableProperty]
        private int pendingJobs;

        [ObservableProperty]
        private int completedJobsToday;

        [ObservableProperty]
        private int totalCustomers;

        [ObservableProperty]
        private int lowStockCount;

        [ObservableProperty]
        private bool hasLowStock;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private ObservableCollection<Transaction> pendingTransactions = new();

        [ObservableProperty]
        private bool hasPendingTransactions;

        public DashboardViewModel(WashTrackContext context)
        {
            _context = context;
        }

        [RelayCommand]
        public async Task LoadDashboardAsync()
        {
            IsLoading = true;

            var today = DateTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);

            // Total sales today
            TotalSalesToday = await _context.Transactions
                .Where(t => t.CreatedAt.Date == today && t.Status == "Completed")
                .SumAsync(t => t.TotalCost);

            // Total sales this month
            TotalSalesThisMonth = await _context.Transactions
                .Where(t => t.CreatedAt >= startOfMonth && t.Status == "Completed")
                .SumAsync(t => t.TotalCost);

            // Pending jobs
            PendingJobs = await _context.Transactions
                .CountAsync(t => t.Status == "Pending");

            // Completed today
            CompletedJobsToday = await _context.Transactions
                .CountAsync(t => t.CreatedAt.Date == today && t.Status == "Completed");

            // Total customers
            TotalCustomers = await _context.Customers.CountAsync();

            // Low stock
            var lowStock = await _context.Inventories
                .Where(i => i.CurrentStock <= i.MinimumThreshold)
                .CountAsync();

            LowStockCount = lowStock;
            HasLowStock = lowStock > 0;

            var pending = await _context.Transactions
                .Include(t => t.Customer)
                .Include(t => t.Items)
                .Where(t => t.Status == "Pending")
                .OrderBy(t => t.CreatedAt)
                .ToListAsync();

            PendingTransactions = new ObservableCollection<Transaction>(pending);
            HasPendingTransactions = pending.Count > 0;

            IsLoading = false;
        }

        [RelayCommand]
        public async Task GoToTransactionsAsync()
        {
            await Shell.Current.GoToAsync("///TransactionsPage");
        }

        [RelayCommand]
        public async Task GoToInventoryAsync()
        {
            await Shell.Current.GoToAsync("///InventoryPage");
        }

        [RelayCommand]
        public async Task GoToServicesAsync()
        {
            await Shell.Current.GoToAsync("///ServicesPage");
        }

        [RelayCommand]
        public async Task GoToReportsAsync()
        {
            await Shell.Current.GoToAsync("///ReportsPage");
        }
    }
}