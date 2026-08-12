using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using WashTrack.Data;
using WashTrack.Models;

namespace WashTrack.MVVM.ViewModels
{
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

            IsLoading = false;
        }

        [RelayCommand]
        public async Task FilterAsync()
        {
            await LoadReportsAsync();
        }
    }
}