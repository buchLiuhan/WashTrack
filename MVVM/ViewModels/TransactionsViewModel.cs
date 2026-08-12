using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using WashTrack.Data;
using WashTrack.Models;
using WashTrack.MVVM.Views;

namespace WashTrack.MVVM.ViewModels
{
    public partial class TransactionsViewModel : ObservableObject
    {
        private readonly WashTrackContext _context;

        [ObservableProperty]
        private ObservableCollection<Transaction> transactions = new();

        [ObservableProperty]
        private bool isLoading;

        public TransactionsViewModel(WashTrackContext context)
        {
            _context = context;
        }

        [ObservableProperty]
        private bool showingCompleted = false;

        [ObservableProperty]
        private string toggleButtonText = "Show Completed";

        [RelayCommand]
        public async Task ToggleCompletedAsync()
        {
            ShowingCompleted = !ShowingCompleted;
            ToggleButtonText = ShowingCompleted ? "Show Pending" : "Show Completed";
            await LoadTransactionsAsync();
        }

        [RelayCommand]
        public async Task CollectPaymentAsync(Transaction transaction)
        {
            var parameters = new Dictionary<string, object>
            {
                { "Transaction", transaction },
                { "CollectingPayment", true }
            };
            await Shell.Current.GoToAsync(nameof(TransactionDetailPage), parameters);
        }

        [RelayCommand]
        public async Task OpenWashStatusAsync(Transaction transaction)
        {
            var parameters = new Dictionary<string, object>
            {
                { "Transaction", transaction },
                { "WashMode", true }
            };
            await Shell.Current.GoToAsync(nameof(TransactionDetailPage), parameters);
        }

        [RelayCommand]
        public async Task LoadTransactionsAsync()
        {
            IsLoading = true;
            var list = await _context.Transactions
                .Include(t => t.Customer)
                .Include(t => t.Items)
                    .ThenInclude(i => i.Service)
                .Where(t => (t.Status == "Completed") == ShowingCompleted)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            Transactions.Clear();
            foreach (var t in list)
                Transactions.Add(t);

            IsLoading = false;
        }

        [RelayCommand]
        public async Task AddTransactionAsync()
        {
            await Shell.Current.GoToAsync(nameof(TransactionDetailPage));
        }

        [RelayCommand]
        public async Task ViewTransactionAsync(Transaction transaction)
        {
            var parameters = new Dictionary<string, object>
            {
                { "Transaction", transaction }
            };
            await Shell.Current.GoToAsync(nameof(TransactionDetailPage), parameters);
        }

        [RelayCommand]
        public async Task CompleteOrderAsync(Transaction transaction)
        {
            bool isPaid = transaction.IsPaid;
            bool isWashed = transaction.WashStatus == "Washed";

            if (!isPaid || !isWashed)
            {
                string message = "";
                if (!isPaid) message += "Payment is still pending.\n";
                if (!isWashed) message += "Clothes aren't washed yet.";
                await Shell.Current.DisplayAlert("Not ready yet", message.Trim(), "OK");
                return;
            }

            bool confirm = await Shell.Current.DisplayAlert(
                "Complete Transaction",
                $"Mark transaction for '{transaction.Customer?.Name}' as completed?",
                "Yes", "No");
            if (!confirm) return;

            transaction.Status = "Completed";
            transaction.CompletedAt = DateTime.Now;
            _context.Transactions.Update(transaction);
            await _context.SaveChangesAsync();
            await LoadTransactionsAsync();
        }

        [RelayCommand]
        public async Task DeleteTransactionAsync(Transaction transaction)
        {
            bool confirm = await Shell.Current.DisplayAlert(
                "Delete Transaction",
                "Are you sure you want to delete this transaction?",
                "Yes", "No");

            if (!confirm) return;

            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync();
            await LoadTransactionsAsync();
        }
    }
}