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

        [ObservableProperty]
        private bool showingActiveOrdersOnly = false;

        [ObservableProperty]
        private string ordersToggleButtonText = "Active Orders Only";

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
            ApplyFilters();
            IsLoading = false;
        }

        // Search and the active-orders filter both narrow the same loaded
        // list, so they need to combine rather than each overwriting the
        // other's result.
        private void ApplyFilters()
        {
            IEnumerable<CustomerWithOrders> filtered = _allCustomerOrders;

            if (ShowingActiveOrdersOnly)
                filtered = filtered.Where(c => c.HasActiveOrders);

            if (!string.IsNullOrWhiteSpace(SearchText))
                filtered = filtered.Where(c =>
                    c.Customer.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    c.Customer.ContactNumber.Contains(SearchText));

            CustomerOrders = new ObservableCollection<CustomerWithOrders>(filtered);
        }

        [RelayCommand]
        public async Task ToggleInactiveAsync()
        {
            ShowingInactive = !ShowingInactive;
            ToggleButtonText = ShowingInactive ? "Show Active" : "Show Inactive";
            SearchText = string.Empty;

            // The active-orders filter only makes sense on the active list.
            ShowingActiveOrdersOnly = false;
            OrdersToggleButtonText = "Active Orders Only";

            await LoadCustomersAsync();
        }

        // One button, two states: narrows the loaded list to customers with
        // an open order, without hitting the database again.
        [RelayCommand]
        public void ToggleActiveOrdersOnly()
        {
            ShowingActiveOrdersOnly = !ShowingActiveOrdersOnly;
            OrdersToggleButtonText = ShowingActiveOrdersOnly ? "All Customers" : "Active Orders Only";
            ApplyFilters();
        }

        partial void OnSearchTextChanged(string value) => ApplyFilters();

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