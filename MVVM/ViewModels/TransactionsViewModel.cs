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

        [ObservableProperty]
        private bool showingCompleted = false;

        [ObservableProperty]
        private string toggleButtonText = "Show Completed";

        public TransactionsViewModel(WashTrackContext context)
        {
            _context = context;
        }

        // ===== LIST LOADING =====

        // Loads the list for the current tab (Pending or Completed).
        // AsNoTracking is required here: without it EF returns the same
        // cached objects and the pills never visually refresh.
        [RelayCommand]
        public async Task LoadTransactionsAsync()
        {
            IsLoading = true;
            var list = await _context.Transactions
                .AsNoTracking()
                .Include(t => t.Customer)
                .Include(t => t.Items)
                    .ThenInclude(i => i.Service)
                .Where(t => (t.Status == "Completed") == ShowingCompleted)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            Transactions = new ObservableCollection<Transaction>(list);
            IsLoading = false;
        }

        // Switches between the Pending and Completed tabs.
        [RelayCommand]
        public async Task ToggleCompletedAsync()
        {
            ShowingCompleted = !ShowingCompleted;
            ToggleButtonText = ShowingCompleted ? "Show Pending" : "Show Completed";
            await LoadTransactionsAsync();
        }

        // ===== NAVIGATION =====

        // Opens a blank detail page for a brand new transaction.
        [RelayCommand]
        public async Task AddTransactionAsync()
        {
            await Shell.Current.GoToAsync(nameof(TransactionDetailPage));
        }

        // Tapping the card body: edit if pending, read-only receipt if completed.
        [RelayCommand]
        public async Task ViewTransactionAsync(Transaction transaction)
        {
            var parameters = new Dictionary<string, object>
            {
                { "Transaction", transaction }
            };
            await Shell.Current.GoToAsync(nameof(TransactionDetailPage), parameters);
        }

        // Tapping the orange "Pending payment" pill.
        // This is the ONLY place payment can be collected.
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

        // Tapping the wash status pill.
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

        // ===== COMPLETING AN ORDER =====

        // Always tappable. If payment or washing isn't done, this shows a
        // reminder instead of completing. Only both-done reaches the confirm.
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
                $"Mark transaction for '{transaction.Customer?.Name}' as completed?\n\nOnce completed it becomes a permanent record and can no longer be edited or deleted.",
                "Yes", "No");
            if (!confirm) return;

            // Re-fetch tracked, because the list was loaded with AsNoTracking.
            var tracked = await _context.Transactions
                .FirstOrDefaultAsync(t => t.TransactionId == transaction.TransactionId);
            if (tracked == null) return;

            tracked.Status = "Completed";
            tracked.CompletedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            await LoadTransactionsAsync();
        }

        // ===== DELETING =====

        // Only reachable for Pending transactions — the XAML hides this
        // button once completed, since finalised records must stay intact.
        [RelayCommand]
        public async Task DeleteTransactionAsync(Transaction transaction)
        {
            bool confirm = await Shell.Current.DisplayAlert(
                "Delete Transaction",
                "Are you sure you want to delete this transaction?",
                "Yes", "No");

            if (!confirm) return;

            // Re-fetch tracked (with its items) so EF can cascade the delete.
            var tracked = await _context.Transactions
                .Include(t => t.Items)
                .FirstOrDefaultAsync(t => t.TransactionId == transaction.TransactionId);
            if (tracked == null) return;

            _context.Transactions.Remove(tracked);
            await _context.SaveChangesAsync();
            await LoadTransactionsAsync();
        }
    }
}