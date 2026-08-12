using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using WashTrack.Data;
using WashTrack.Messages;
using WashTrack.Models;
using WashTrack.MVVM.Views;

namespace WashTrack.MVVM.ViewModels
{
    [QueryProperty(nameof(Transaction), "Transaction")]
    [QueryProperty(nameof(CollectingPayment), "CollectingPayment")]
    [QueryProperty(nameof(WashMode), "WashMode")]
    public partial class TransactionDetailViewModel : ObservableObject
    {
        private readonly WashTrackContext _context;

        [ObservableProperty] private Transaction transaction = new();
        [ObservableProperty] private string title = "New Transaction";
        [ObservableProperty] private ObservableCollection<Customer> customers = new();
        [ObservableProperty] private ObservableCollection<Customer> filteredCustomers = new();
        [ObservableProperty] private ObservableCollection<Service> services = new();
        [ObservableProperty] private ObservableCollection<TransactionItem> cartItems = new();
        [ObservableProperty] private Customer? selectedCustomer;
        [ObservableProperty] private Service? selectedService;
        [ObservableProperty] private string customerSearch = string.Empty;
        [ObservableProperty] private string selectedCustomerName = string.Empty;
        [ObservableProperty] private bool showCustomerSuggestions = false;
        [ObservableProperty] private bool showAddCustomerOption = false;
        [ObservableProperty] private bool showAddAddressOption = false;
        [ObservableProperty] private string weightKg = string.Empty;
        [ObservableProperty] private decimal cartTotal;
        [ObservableProperty] private string status = "Pending";
        [ObservableProperty] private bool isViewMode = false;
        [ObservableProperty] private bool hasCartItems = false;
        [ObservableProperty] private ObservableCollection<string> fulfillmentOptions = new() { "Pickup", "Delivery" };
        [ObservableProperty] private string selectedFulfillmentType = "Pickup";
        [ObservableProperty] private ObservableCollection<string> paymentOptions = new() { "Pay Now", "Pay Later" };
        [ObservableProperty] private string selectedPaymentType = "Pay Now";
        [ObservableProperty] private string cashReceived = string.Empty;
        [ObservableProperty] private decimal change;
        [ObservableProperty] private decimal amountPaid;
        [ObservableProperty] private bool collectingPayment = false;

        [ObservableProperty] private bool washMode = false;
        [ObservableProperty] private ObservableCollection<string> washStatusOptions = new() { "To Be Washed", "Washing", "Washed" };
        [ObservableProperty] private string selectedWashStatus = "To Be Washed";

        private bool _suppressSearch = false;
        private int _existingTransactionId = 0;

        public string SaveButtonText
        {
            get
            {
                if (CollectingPayment) return "Confirm Payment";
                return _existingTransactionId != 0 ? "Save Changes" : "Complete Transaction";
            }
        }

        public bool ShowFullEditSection => !CollectingPayment && !WashMode;
        public bool ShowWashSection => WashMode;
        public bool ShowMainActionButtons => !IsViewMode && !WashMode;

        private void RefreshModeVisibility()
        {
            OnPropertyChanged(nameof(ShowFullEditSection));
            OnPropertyChanged(nameof(ShowWashSection));
            OnPropertyChanged(nameof(ShowMainActionButtons));
        }

        public TransactionDetailViewModel(WashTrackContext context)
        {
            _context = context;

            WeakReferenceMessenger.Default.Register<CustomerCreatedMessage>(this, (recipient, message) =>
            {
                var vm = (TransactionDetailViewModel)recipient;
                var incoming = message.Value;

                var existing = vm.Customers.FirstOrDefault(c => c.CustomerId == incoming.CustomerId);
                if (existing != null)
                    vm.Customers.Remove(existing);

                vm.Customers.Add(incoming);
                vm.SelectCustomer(incoming);
                vm.RecalculateAddressPrompt();
            });
        }

        partial void OnTransactionChanged(Transaction value)
        {
            if (value != null && value.TransactionId != 0)
            {
                _existingTransactionId = value.TransactionId;
                IsViewMode = value.Status == "Completed";
                Title = IsViewMode ? "Receipt" : "Edit Transaction";
                Status = value.Status;
                OnPropertyChanged(nameof(SaveButtonText));
                RefreshModeVisibility();
            }
        }

        partial void OnCollectingPaymentChanged(bool value)
        {
            OnPropertyChanged(nameof(SaveButtonText));
            RefreshModeVisibility();
        }

        partial void OnWashModeChanged(bool value)
        {
            RefreshModeVisibility();
        }

        [RelayCommand]
        public async Task LoadDataAsync()
        {
            var customerList = await _context.Customers
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
            Customers = new ObservableCollection<Customer>(customerList);

            var serviceList = await _context.Services
                .Where(s => s.IsActive)
                .OrderBy(s => s.ServiceName)
                .ToListAsync();
            Services = new ObservableCollection<Service>(serviceList);

            if (_existingTransactionId != 0)
            {
                await LoadExistingTransactionAsync(_existingTransactionId);
            }
        }

        private async Task LoadExistingTransactionAsync(int transactionId)
        {
            try
            {
                var full = await _context.Transactions
                    .Include(t => t.Customer)
                    .Include(t => t.Items)
                        .ThenInclude(i => i.Service)
                    .FirstOrDefaultAsync(t => t.TransactionId == transactionId);

                if (full == null) return;

                CartItems = new ObservableCollection<TransactionItem>(full.Items);
                HasCartItems = CartItems.Count > 0;
                CartTotal = full.TotalCost;
                SelectedFulfillmentType = full.FulfillmentType;
                SelectedPaymentType = full.PaymentType;
                AmountPaid = full.AmountPaid ?? 0;
                SelectedWashStatus = full.WashStatus;

                if (full.Customer != null)
                {
                    SelectedCustomer = Customers.FirstOrDefault(c => c.CustomerId == full.Customer.CustomerId) ?? full.Customer;
                    _suppressSearch = true;
                    CustomerSearch = full.Customer.Name;
                    SelectedCustomerName = full.Customer.Name;
                    _suppressSearch = false;
                }

                if (CollectingPayment)
                {
                    SelectedPaymentType = "Pay Now";
                    Title = "Collect Payment";
                }

                if (WashMode)
                {
                    Title = "Update Wash Status";
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Couldn't load this transaction: {ex.Message}", "OK");
            }
        }

        partial void OnCustomerSearchChanged(string value)
        {
            if (_suppressSearch) return;

            if (string.IsNullOrWhiteSpace(value))
            {
                FilteredCustomers = new ObservableCollection<Customer>();
                ShowCustomerSuggestions = false;
                ShowAddCustomerOption = false;
                return;
            }

            var filtered = Customers
                .Where(c => c.Name.Contains(value, StringComparison.OrdinalIgnoreCase))
                .ToList();

            FilteredCustomers = new ObservableCollection<Customer>(filtered);
            ShowCustomerSuggestions = filtered.Count > 0;
            ShowAddCustomerOption = filtered.Count == 0 && value.Trim().Length >= 2;
        }

        partial void OnSelectedServiceChanged(Service? value)
        {
            OnPropertyChanged(nameof(QuantityLabel));
        }

        partial void OnSelectedFulfillmentTypeChanged(string value) => RecalculateAddressPrompt();

        partial void OnSelectedCustomerChanged(Customer? value) => RecalculateAddressPrompt();

        partial void OnSelectedPaymentTypeChanged(string value)
        {
            OnPropertyChanged(nameof(IsPayNow));
            OnPropertyChanged(nameof(IsPayLater));
            CashReceived = string.Empty;
            Change = 0;
        }

        partial void OnCashReceivedChanged(string value) => RecalculateChange();

        partial void OnCartTotalChanged(decimal value) => RecalculateChange();

        public bool IsPayNow => SelectedPaymentType == "Pay Now";
        public bool IsPayLater => SelectedPaymentType == "Pay Later";

        private void RecalculateChange()
        {
            if (!decimal.TryParse(CashReceived, out decimal received))
            {
                Change = 0;
                return;
            }
            Change = received - CartTotal;
        }

        public void RecalculateAddressPrompt()
        {
            ShowAddAddressOption = !IsViewMode
                && SelectedFulfillmentType == "Delivery"
                && SelectedCustomer != null
                && string.IsNullOrWhiteSpace(SelectedCustomer.Address);
        }

        [RelayCommand]
        public void SelectCustomer(Customer customer)
        {
            SelectedCustomer = customer;
            SelectedCustomerName = customer.Name;

            _suppressSearch = true;
            CustomerSearch = customer.Name;
            _suppressSearch = false;

            ShowCustomerSuggestions = false;
            ShowAddCustomerOption = false;
        }

        [RelayCommand]
        public async Task GoToAddCustomerAsync()
        {
            var parameters = new Dictionary<string, object>
            {
                { "PrefillName", CustomerSearch.Trim() },
                { "FromTransaction", true }
            };
            await Shell.Current.GoToAsync(nameof(CustomerDetailPage), parameters);
        }

        [RelayCommand]
        public async Task GoToAddAddressAsync()
        {
            if (SelectedCustomer == null) return;

            var parameters = new Dictionary<string, object>
            {
                { "Customer", SelectedCustomer },
                { "FromTransaction", true }
            };
            await Shell.Current.GoToAsync(nameof(CustomerDetailPage), parameters);
        }

        public string QuantityLabel => SelectedService?.FlatRate.HasValue == true
            ? "Quantity (pieces)"
            : "Weight (kg)";

        private decimal CalculateLineCost(Service service, decimal enteredValue)
        {
            if (service.FlatRate.HasValue)
                return service.FlatRate.Value * enteredValue;

            if (service.MinKilo.HasValue)
            {
                if (enteredValue <= service.MinKilo.Value)
                    return service.MinKiloCharge ?? 0;

                decimal excess = enteredValue - service.MinKilo.Value;
                return (service.MinKiloCharge ?? 0) + (excess * (service.ExcessPerKilo ?? 0));
            }

            return (service.PricePerKilo ?? 0) * enteredValue;
        }

        [RelayCommand]
        public void AddServiceToCart()
        {
            if (SelectedService == null)
            {
                Shell.Current.DisplayAlert("Error", "Please select a service.", "OK");
                return;
            }

            if (!decimal.TryParse(WeightKg, out decimal enteredValue) || enteredValue <= 0)
            {
                string label = SelectedService.FlatRate.HasValue ? "quantity" : "weight";
                Shell.Current.DisplayAlert("Error", $"Please enter a valid {label}.", "OK");
                return;
            }

            var existingItem = CartItems.FirstOrDefault(i => i.ServiceId == SelectedService.ServiceId);

            if (existingItem != null)
            {
                decimal combinedWeight = existingItem.WeightKg + enteredValue;
                decimal recalculatedCost = CalculateLineCost(SelectedService, combinedWeight);

                int index = CartItems.IndexOf(existingItem);
                CartItems[index] = new TransactionItem
                {
                    ServiceId = SelectedService.ServiceId,
                    Service = SelectedService,
                    WeightKg = combinedWeight,
                    LineCost = recalculatedCost
                };
            }
            else
            {
                var lineCost = CalculateLineCost(SelectedService, enteredValue);
                CartItems.Add(new TransactionItem
                {
                    ServiceId = SelectedService.ServiceId,
                    Service = SelectedService,
                    WeightKg = enteredValue,
                    LineCost = lineCost
                });
            }

            HasCartItems = true;
            RecalculateCartTotal();

            SelectedService = null;
            WeightKg = string.Empty;
        }

        [RelayCommand]
        public void RemoveCartItem(TransactionItem item)
        {
            CartItems.Remove(item);
            HasCartItems = CartItems.Count > 0;
            RecalculateCartTotal();
        }

        private void RecalculateCartTotal()
        {
            CartTotal = CartItems.Sum(i => i.LineCost);
        }

        [RelayCommand]
        public async Task ConfirmWashStatusAsync()
        {
            if (_existingTransactionId == 0) return;

            var existing = await _context.Transactions.FindAsync(_existingTransactionId);
            if (existing == null) return;

            existing.WashStatus = SelectedWashStatus;
            await _context.SaveChangesAsync();
            await Shell.Current.GoToAsync("..");
        }

        [RelayCommand]
        public async Task CompleteTransactionAsync()
        {
            if (SelectedCustomer == null)
            {
                await Shell.Current.DisplayAlert("Error", "Please select or add a customer.", "OK");
                return;
            }

            if (CartItems.Count == 0)
            {
                await Shell.Current.DisplayAlert("Error", "Add at least one service before completing.", "OK");
                return;
            }

            if (SelectedFulfillmentType == "Delivery" && string.IsNullOrWhiteSpace(SelectedCustomer.Address))
            {
                await Shell.Current.DisplayAlert("Error",
                    "This customer has no address on file. Please add one before completing a delivery order.", "OK");
                return;
            }

            decimal finalAmountPaid;

            if (SelectedPaymentType == "Pay Now")
            {
                if (!decimal.TryParse(CashReceived, out decimal receivedValue) || receivedValue < CartTotal)
                {
                    await Shell.Current.DisplayAlert("Error", "Cash received must cover the total.", "OK");
                    return;
                }
                finalAmountPaid = CartTotal;
            }
            else
            {
                finalAmountPaid = 0;
            }

            var itemsSummary = string.Join("\n",
                CartItems.Select(i => $"{i.Service?.ServiceName} — {i.WeightKg}kg — ₱{i.LineCost:F2}"));
            var paymentLine = SelectedPaymentType == "Pay Now"
                ? $"Payment: Pay Now (Change: ₱{Change:F2})"
                : "Payment: Pay Later";
            var confirmMessage =
                $"Customer: {SelectedCustomer.Name}\nFulfillment: {SelectedFulfillmentType}\n{paymentLine}\n\n{itemsSummary}\n\nTotal: ₱{CartTotal:F2}";

            bool confirmed = await Shell.Current.DisplayAlert("Confirm Transaction", confirmMessage, "Confirm", "Cancel");
            if (!confirmed) return;

            if (_existingTransactionId != 0)
            {
                var existing = await _context.Transactions
                    .Include(t => t.Items)
                    .FirstOrDefaultAsync(t => t.TransactionId == _existingTransactionId);

                if (existing == null) return;

                existing.CustomerId = SelectedCustomer.CustomerId;
                existing.FulfillmentType = SelectedFulfillmentType;
                existing.PaymentType = SelectedPaymentType;
                existing.AmountPaid = finalAmountPaid;
                _context.TransactionItems.RemoveRange(existing.Items);
                existing.Items.Clear();

                foreach (var item in CartItems)
                {
                    existing.Items.Add(new TransactionItem
                    {
                        ServiceId = item.ServiceId,
                        WeightKg = item.WeightKg,
                        LineCost = item.LineCost
                    });
                }
                existing.TotalCost = CartItems.Sum(i => i.LineCost);

                await _context.SaveChangesAsync();
                await Shell.Current.GoToAsync("..");
                return;
            }

            var newTransaction = new Transaction
            {
                CustomerId = SelectedCustomer.CustomerId,
                Status = "Pending",
                FulfillmentType = SelectedFulfillmentType,
                PaymentType = SelectedPaymentType,
                AmountPaid = finalAmountPaid,
                TotalCost = CartItems.Sum(i => i.LineCost),
                CreatedAt = DateTime.Now
            };

            foreach (var item in CartItems)
            {
                newTransaction.Items.Add(new TransactionItem
                {
                    ServiceId = item.ServiceId,
                    WeightKg = item.WeightKg,
                    LineCost = item.LineCost
                });
            }

            await _context.Transactions.AddAsync(newTransaction);

            var trackedCustomer = await _context.Customers.FindAsync(SelectedCustomer.CustomerId);
            if (trackedCustomer != null)
            {
                trackedCustomer.LastTransaction = DateTime.Now;
            }
            else
            {
                SelectedCustomer.LastTransaction = DateTime.Now;
                _context.Customers.Update(SelectedCustomer);
            }

            await _context.SaveChangesAsync();
            await Shell.Current.GoToAsync("..");
        }

        [RelayCommand]
        public async Task GoToServicesAsync()
        {
            await Shell.Current.GoToAsync("///ServicesPage");
        }

        [RelayCommand]
        public async Task CancelAsync()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}