using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using WashTrack.Data;
using WashTrack.Models;
using WashTrack.MVVM.Views;

namespace WashTrack.MVVM.ViewModels
{
    public partial class CustomerWithOrders : ObservableObject
    {
        public Customer Customer { get; set; } = new();
        public List<Transaction> ActiveOrders { get; set; } = new();
        public bool HasActiveOrders => ActiveOrders.Count > 0;
        public bool CanDelete => !HasActiveOrders;
        public bool HasPendingPayment => ActiveOrders.Any(o => (o.AmountPaid ?? 0) < o.TotalCost);
    }

    public partial class CustomersViewModel : ObservableObject
    {
        private readonly WashTrackContext _context;

        [ObservableProperty]
        private ObservableCollection<CustomerWithOrders> customerOrders = new();

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string searchText = string.Empty;

        [ObservableProperty]
        private bool showingInactive = false;

        [ObservableProperty]
        private string toggleButtonText = "Show Inactive";

        private List<CustomerWithOrders> _allCustomerOrders = new();

        public CustomersViewModel(WashTrackContext context)
        {
            _context = context;
        }

        [RelayCommand]
        public async Task LoadCustomersAsync()
        {
            IsLoading = true;

            var customers = await _context.Customers
                .Where(c => c.IsActive != ShowingInactive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            var result = new List<CustomerWithOrders>();

            foreach (var customer in customers)
            {
                var activeOrders = await _context.Transactions
                   .Include(t => t.Items)
                       .ThenInclude(i => i.Service)
                   .Where(t => t.CustomerId == customer.CustomerId
                    && t.Status == "Pending")
                   .OrderBy(t => t.CreatedAt)
                   .ToListAsync();

                result.Add(new CustomerWithOrders
                {
                    Customer = customer,
                    ActiveOrders = activeOrders
                });
            }

            _allCustomerOrders = result;
            CustomerOrders = new ObservableCollection<CustomerWithOrders>(result);
            IsLoading = false;
        }

        [RelayCommand]
        public async Task ToggleInactiveAsync()
        {
            ShowingInactive = !ShowingInactive;
            ToggleButtonText = ShowingInactive ? "Show Active" : "Show Inactive";
            SearchText = string.Empty;
            await LoadCustomersAsync();
        }

        partial void OnSearchTextChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                CustomerOrders = new ObservableCollection<CustomerWithOrders>(_allCustomerOrders);
                return;
            }

            var filtered = _allCustomerOrders
                .Where(c => c.Customer.Name.Contains(value, StringComparison.OrdinalIgnoreCase) ||
                            c.Customer.ContactNumber.Contains(value))
                .ToList();

            CustomerOrders = new ObservableCollection<CustomerWithOrders>(filtered);
        }

        [RelayCommand]
        public async Task EditCustomerAsync(Customer customer)
        {
            var parameters = new Dictionary<string, object>
            {
                { "Customer", customer }
            };
            await Shell.Current.GoToAsync(nameof(CustomerDetailPage), parameters);
        }

        [RelayCommand]
        public async Task MarkOrderDoneAsync(Transaction transaction)
        {
            bool confirm = await Shell.Current.DisplayAlert(
                "Mark as Done",
                "Mark this order as completed?",
                "Yes", "No");

            if (!confirm) return;

            transaction.Status = "Completed";
            transaction.CompletedAt = DateTime.Now;
            _context.Transactions.Update(transaction);
            await _context.SaveChangesAsync();
            await LoadCustomersAsync();
        }

        [RelayCommand]
        public async Task DeactivateCustomerAsync(CustomerWithOrders customerOrder)
        {
            bool confirm = await Shell.Current.DisplayAlert(
                "Deactivate Customer",
                $"Move '{customerOrder.Customer.Name}' to inactive? Their history will be preserved.",
                "Yes", "No");

            if (!confirm) return;

            customerOrder.Customer.IsActive = false;
            _context.Customers.Update(customerOrder.Customer);
            await _context.SaveChangesAsync();
            await LoadCustomersAsync();
        }

        [RelayCommand]
        public async Task RestoreCustomerAsync(CustomerWithOrders customerOrder)
        {
            customerOrder.Customer.IsActive = true;
            _context.Customers.Update(customerOrder.Customer);
            await _context.SaveChangesAsync();
            await LoadCustomersAsync();
        }

        [RelayCommand]
        public async Task PermanentDeleteAsync(CustomerWithOrders customerOrder)
        {
            bool confirm = await Shell.Current.DisplayAlert(
                "Permanently Delete",
                $"This will permanently delete '{customerOrder.Customer.Name}' and ALL their transaction history. This cannot be undone.",
                "Delete Forever", "Cancel");

            if (!confirm) return;

            var transactions = await _context.Transactions
                .Where(t => t.CustomerId == customerOrder.Customer.CustomerId)
                .ToListAsync();

            _context.Transactions.RemoveRange(transactions);
            _context.Customers.Remove(customerOrder.Customer);
            await _context.SaveChangesAsync();
            await LoadCustomersAsync();
        }
    }
}