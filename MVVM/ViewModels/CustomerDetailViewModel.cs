using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using WashTrack.Data;
using WashTrack.Messages;
using WashTrack.Models;

namespace WashTrack.MVVM.ViewModels
{
    [QueryProperty(nameof(Customer), "Customer")]
    [QueryProperty(nameof(PrefillName), "PrefillName")]
    [QueryProperty(nameof(FromTransaction), "FromTransaction")]
    public partial class CustomerDetailViewModel : ObservableObject
    {
        private readonly WashTrackContext _context;

        [ObservableProperty] private Customer customer = new();
        [ObservableProperty] private string title = "Customer Details";
        [ObservableProperty] private string name = string.Empty;
        [ObservableProperty] private string contactNumber = string.Empty;
        [ObservableProperty] private string email = string.Empty;
        [ObservableProperty] private string address = string.Empty;
        [ObservableProperty] private string prefillName = string.Empty;
        [ObservableProperty] private bool fromTransaction = false;

        [ObservableProperty] private ObservableCollection<Transaction> pendingTransactions = new();
        [ObservableProperty] private ObservableCollection<Transaction> completedTransactions = new();
        [ObservableProperty] private bool hasPending;
        [ObservableProperty] private bool hasCompleted;
        [ObservableProperty] private decimal totalSpent;

        private bool _isEditing = false;

        public string SaveButtonText => FromTransaction ? "Save & Return to Transaction" : "Save Changes";

        public CustomerDetailViewModel(WashTrackContext context)
        {
            _context = context;
        }

        partial void OnFromTransactionChanged(bool value)
        {
            OnPropertyChanged(nameof(SaveButtonText));
            if (_isEditing)
                Title = value ? "Input Address" : Customer.Name;
        }

        partial void OnPrefillNameChanged(string value)
        {
            if (!_isEditing && !string.IsNullOrWhiteSpace(value))
                Name = value;
        }

        partial void OnCustomerChanged(Customer value)
        {
            if (value != null && value.CustomerId != 0)
            {
                _isEditing = true;
                Title = FromTransaction ? "Input Address" : value.Name;
                Name = value.Name;
                ContactNumber = value.ContactNumber;
                Email = value.Email ?? string.Empty;
                Address = value.Address ?? string.Empty;
                LoadTransactionsAsync().ConfigureAwait(false);
            }
        }

        private async Task LoadTransactionsAsync()
        {
            var allTransactions = await _context.Transactions
                .Include(t => t.Items)
                    .ThenInclude(i => i.Service)
                .Where(t => t.CustomerId == Customer.CustomerId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            var pending = allTransactions.Where(t => t.Status == "Pending").ToList();
            var completed = allTransactions.Where(t => t.Status == "Completed").ToList();

            PendingTransactions = new ObservableCollection<Transaction>(pending);
            CompletedTransactions = new ObservableCollection<Transaction>(completed);

            HasPending = pending.Count > 0;
            HasCompleted = completed.Count > 0;
            TotalSpent = completed.Sum(t => t.TotalCost);
        }

        [RelayCommand]
        public async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                await Shell.Current.DisplayAlert("Error", "Name is required.", "OK");
                return;
            }
            if (string.IsNullOrWhiteSpace(ContactNumber))
            {
                await Shell.Current.DisplayAlert("Error", "Contact number is required.", "OK");
                return;
            }
            if (string.IsNullOrWhiteSpace(Address))
            {
                await Shell.Current.DisplayAlert("Error", "Address is required.", "OK");
                return;
            }

            var confirmMessage = $"Name: {Name}\nContact: {ContactNumber}\nAddress: {Address}";
            bool confirmed = await Shell.Current.DisplayAlert("Confirm Details", confirmMessage, "Confirm", "Cancel");
            if (!confirmed) return;

            if (_isEditing)
            {
                Customer.Name = Name;
                Customer.ContactNumber = ContactNumber;
                Customer.Email = Email;
                Customer.Address = Address;
                _context.Customers.Update(Customer);
                await _context.SaveChangesAsync();

                if (FromTransaction)
                    WeakReferenceMessenger.Default.Send(new CustomerCreatedMessage(Customer));
            }
            else
            {
                var newCustomer = new Customer
                {
                    Name = Name,
                    ContactNumber = ContactNumber,
                    Email = Email,
                    Address = Address,
                    CreatedAt = DateTime.Now
                };
                await _context.Customers.AddAsync(newCustomer);
                await _context.SaveChangesAsync();

                if (FromTransaction)
                    WeakReferenceMessenger.Default.Send(new CustomerCreatedMessage(newCustomer));
            }

            await Shell.Current.GoToAsync("..");
        }

        [RelayCommand]
        public async Task CancelAsync()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}