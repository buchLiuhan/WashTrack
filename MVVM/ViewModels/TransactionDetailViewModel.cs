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

        // ===== BOUND STATE =====

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
        [ObservableProperty] private DateTime createdAt = DateTime.Now;

        // Wash status mode
        [ObservableProperty] private bool washMode = false;
        [ObservableProperty] private ObservableCollection<string> washStatusOptions = new();
        [ObservableProperty] private string selectedWashStatus = "To Be Washed";
        private string _currentWashStatus = "To Be Washed";

        private bool _suppressSearch = false;
        private int _existingTransactionId = 0;

        // ===== MODE FLAGS =====
        // These decide which parts of the page are shown or locked.

        // True when we're editing a transaction that already exists.
        public bool IsEditingExisting => _existingTransactionId != 0;

        // True only when creating a brand new transaction — the only time
        // customer, fulfillment and payment type can be chosen.
        public bool CanEditOrderTerms => !IsEditingExisting && !CollectingPayment && !WashMode && !IsViewMode;

        // Services can only be changed while nothing irreversible has happened:
        // no money taken yet, and the clothes haven't gone into the machine.
        // The paid check only counts once there's actually a bill — a brand
        // new transaction starts at 0 and 0, which must not read as "paid".
        public bool CanEditServices =>
            !CollectingPayment
            && !WashMode
            && !IsViewMode
            && !(CartTotal > 0 && AmountPaid >= CartTotal)
            && _currentWashStatus == "To Be Washed";

        // Shows the explanation when service editing has been locked out.
        public bool ShowEditLockNotice => IsEditingExisting && !CanEditServices && !CollectingPayment && !WashMode && !IsViewMode;

        // Tells the owner WHY editing is closed instead of just hiding controls.
        public string EditLockReason
        {
            get
            {
                if (CartTotal > 0 && AmountPaid >= CartTotal)
                    return "This order is already paid and can no longer be edited.";
                if (_currentWashStatus != "To Be Washed")
                    return "This order is being washed and can no longer be edited.";
                return string.Empty;
            }
        }

        // Whole normal section (everything that isn't wash mode).
        public bool ShowFullEditSection => !WashMode;

        public bool ShowWashSection => WashMode;
        public bool ShowMainActionButtons => !IsViewMode && !WashMode;

        // Read-only payment line shown while editing an existing pending order.
        public bool ShowPaymentSummary => IsEditingExisting && !CollectingPayment && !WashMode && !IsViewMode;

        public string PaymentSummaryText =>
            AmountPaid >= CartTotal && CartTotal > 0
                ? $"Paid — ₱{AmountPaid:F2}"
                : $"{SelectedPaymentType} — ₱{CartTotal:F2} due";

        public string SaveButtonText
        {
            get
            {
                if (CollectingPayment) return "Confirm Payment";
                return IsEditingExisting ? "Save Changes" : "Complete Transaction";
            }
        }

        private void RefreshModeVisibility()
        {
            OnPropertyChanged(nameof(IsEditingExisting));
            OnPropertyChanged(nameof(CanEditOrderTerms));
            OnPropertyChanged(nameof(CanEditServices));
            OnPropertyChanged(nameof(ShowEditLockNotice));
            OnPropertyChanged(nameof(EditLockReason));
            OnPropertyChanged(nameof(ShowFullEditSection));
            OnPropertyChanged(nameof(ShowWashSection));
            OnPropertyChanged(nameof(ShowMainActionButtons));
            OnPropertyChanged(nameof(ShowPaymentSummary));
            OnPropertyChanged(nameof(PaymentSummaryText));
            OnPropertyChanged(nameof(SaveButtonText));
            OnPropertyChanged(nameof(ShowCashEntry));
            OnPropertyChanged(nameof(ShowAmountDue));
        }

        public TransactionDetailViewModel(WashTrackContext context)
        {
            _context = context;

            // Fires when a customer is created/edited from inside this flow.
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

        // ===== NAVIGATION PARAMETERS =====

        partial void OnTransactionChanged(Transaction value)
        {
            if (value != null && value.TransactionId != 0)
            {
                _existingTransactionId = value.TransactionId;
                IsViewMode = value.Status == "Completed";
                Title = IsViewMode ? "Receipt" : "Edit Transaction";
                Status = value.Status;
                RefreshModeVisibility();
            }
        }

        partial void OnCollectingPaymentChanged(bool value) => RefreshModeVisibility();

        partial void OnWashModeChanged(bool value) => RefreshModeVisibility();

        // ===== DATA LOADING =====

        // Populates the customer and service pickers, then loads the
        // transaction itself if we're editing an existing one.
        [RelayCommand]
        public async Task LoadDataAsync()
        {
            // AsNoTracking: these two lists are only used to fill pickers.
            var customerList = await _context.Customers
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
            Customers = new ObservableCollection<Customer>(customerList);

            var serviceList = await _context.Services
                .AsNoTracking()
                .Where(s => s.IsActive)
                .OrderBy(s => s.ServiceName)
                .ToListAsync();
            Services = new ObservableCollection<Service>(serviceList);

            if (_existingTransactionId != 0)
            {
                await LoadExistingTransactionAsync(_existingTransactionId);
            }
        }

        // Loads one existing transaction. Stays TRACKED on purpose —
        // CompleteTransactionAsync modifies this same entity later.
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
                CreatedAt = full.CreatedAt;

                _currentWashStatus = full.WashStatus;
                SelectedWashStatus = full.WashStatus;
                BuildWashStatusOptions();

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

                RefreshModeVisibility();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Couldn't load this transaction: {ex.Message}", "OK");
            }
        }

        // ===== WASH STATUS =====

        // Wash status only moves forward: To Be Washed → Washing → Washed.
        // Only the options at or ahead of the current stage are offered.
        private void BuildWashStatusOptions()
        {
            var all = new List<string> { "To Be Washed", "Washing", "Washed" };
            int currentIndex = all.IndexOf(_currentWashStatus);
            if (currentIndex < 0) currentIndex = 0;

            WashStatusOptions = new ObservableCollection<string>(all.Skip(currentIndex));
            SelectedWashStatus = _currentWashStatus;
        }

        // Saves the new wash status. Confirms first, because this is one-way.
        // Moving to "Washing" is also the moment supplies are physically
        // consumed, so that's when stock is deducted.
        [RelayCommand]
        public async Task ConfirmWashStatusAsync()
        {
            if (_existingTransactionId == 0) return;

            // Nothing chosen / unchanged — just leave.
            if (SelectedWashStatus == _currentWashStatus)
            {
                await Shell.Current.GoToAsync("..");
                return;
            }

            string message = SelectedWashStatus == "Washed"
                ? "Mark as washed? This can't be undone."
                : "Start washing this order?";

            bool confirm = await Shell.Current.DisplayAlert(
                "Confirm Wash Status", message, "Yes", "No");
            if (!confirm) return;

            var existing = await _context.Transactions.FindAsync(_existingTransactionId);
            if (existing == null) return;

            bool startingWash = _currentWashStatus == "To Be Washed"
                                && SelectedWashStatus == "Washing";

            existing.WashStatus = SelectedWashStatus;

            // Deduct supplies exactly once, when the wash actually starts.
            if (startingWash)
                await DeductSuppliesAsync(_existingTransactionId);

            await _context.SaveChangesAsync();
            await Shell.Current.GoToAsync("..");
        }

        // ===== SUPPLY DEDUCTION =====

        // Works out how much of each linked supply this order consumes and
        // subtracts it from stock, logging a usage row for the reports.
        // Stock is allowed to go negative: the clothes are being washed
        // regardless, and a negative figure makes the shortage visible.
        private async Task DeductSuppliesAsync(int transactionId)
        {
            var items = await _context.TransactionItems
                .AsNoTracking()
                .Include(i => i.Service)
                .Where(i => i.TransactionId == transactionId)
                .ToListAsync();

            foreach (var item in items)
            {
                var service = item.Service;
                if (service == null) continue;

                // WeightKg holds kilos for weight-based services, or a
                // piece/load count for flat-rate ones. Either way the rate
                // is "per unit", so the multiplication is the same.
                decimal quantity = item.WeightKg;

                await ApplyUsageAsync(service.DetergentItemId, service.DetergentUsage, quantity, transactionId, "Detergent");
                await ApplyUsageAsync(service.ConditionerItemId, service.ConditionerUsage, quantity, transactionId, "Conditioner");
                await ApplyUsageAsync(service.OtherItemId, service.OtherUsage, quantity, transactionId, "Other supply");
            }
        }

        // Subtracts one supply's usage and records it in the history table.
        private async Task ApplyUsageAsync(int? inventoryId, decimal? ratePerUnit, decimal quantity, int transactionId, string slotName)
        {
            // Slot unused, or no rate set — nothing to deduct.
            if (!inventoryId.HasValue || !ratePerUnit.HasValue) return;

            var inventoryItem = await _context.Inventories.FindAsync(inventoryId.Value);
            if (inventoryItem == null) return;

            decimal amountUsed = ratePerUnit.Value * quantity;
            if (amountUsed <= 0) return;

            inventoryItem.CurrentStock -= amountUsed;
            inventoryItem.UpdatedAt = DateTime.Now;

            // History feeds the usage estimate and the inventory reports.
            await _context.InventoryUsageHistories.AddAsync(new InventoryUsageHistory
            {
                InventoryId = inventoryId.Value,
                QuantityUsed = amountUsed,
                UsageDate = DateTime.Now,
                TransactionId = transactionId,
                Notes = $"{slotName} used for order #{transactionId:D4}"
            });
        }

        // ===== CUSTOMER SEARCH =====

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

        // Picks a customer from the suggestion list.
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

        // Opens the customer page to create a new customer mid-transaction.
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

        // Safety valve: lets the owner fill in a missing address for a
        // delivery order without unlocking anything else.
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

        // Shows the "no address on file" warning for delivery orders.
        public void RecalculateAddressPrompt()
        {
            ShowAddAddressOption = !IsViewMode
                && SelectedFulfillmentType == "Delivery"
                && SelectedCustomer != null
                && string.IsNullOrWhiteSpace(SelectedCustomer.Address);
        }

        partial void OnSelectedFulfillmentTypeChanged(string value) => RecalculateAddressPrompt();

        partial void OnSelectedCustomerChanged(Customer? value) => RecalculateAddressPrompt();

        // ===== PAYMENT =====

        public bool IsPayNow => SelectedPaymentType == "Pay Now";
        public bool IsPayLater => SelectedPaymentType == "Pay Later";

        // Money entry only belongs on a brand-new order (CanEditOrderTerms)
        // or the dedicated Collect Payment flow. Plain "edit an existing
        // pending order" mode must never show or touch payment — that's
        // the Pending payment pill's job only.
        public bool ShowCashEntry => IsPayNow && (CanEditOrderTerms || CollectingPayment);
        public bool ShowAmountDue => IsPayLater && (CanEditOrderTerms || CollectingPayment);

        partial void OnSelectedPaymentTypeChanged(string value)
        {
            OnPropertyChanged(nameof(IsPayNow));
            OnPropertyChanged(nameof(IsPayLater));
            OnPropertyChanged(nameof(ShowCashEntry));
            OnPropertyChanged(nameof(ShowAmountDue));
            OnPropertyChanged(nameof(PaymentSummaryText));
            CashReceived = string.Empty;
            Change = 0;
        }

        partial void OnCashReceivedChanged(string value) => RecalculateChange();

        partial void OnCartTotalChanged(decimal value)
        {
            RecalculateChange();
            RefreshModeVisibility();
        }

        partial void OnAmountPaidChanged(decimal value) => RefreshModeVisibility();

        private void RecalculateChange()
        {
            if (!decimal.TryParse(CashReceived, out decimal received))
            {
                Change = 0;
                return;
            }
            Change = received - CartTotal;
        }

        // ===== CART =====

        // Flat-rate services are counted in whole pieces or machine loads.
        // Weight-based services are measured in kilos and allow decimals.
        public string QuantityLabel => SelectedService?.FlatRate.HasValue == true
            ? "Quantity (Pieces/Load)"
            : "Weight (kg)";

        partial void OnSelectedServiceChanged(Service? value)
        {
            OnPropertyChanged(nameof(QuantityLabel));
        }

        // Works out the cost of one line based on the service's pricing mode.
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

        // Adds the chosen service to the cart, merging with an existing line.
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

            // Pieces and machine loads can't be fractional — 3.5 comforters
            // or 2.5 machine loads don't exist. Kilos still allow decimals.
            if (SelectedService.FlatRate.HasValue && enteredValue != Math.Floor(enteredValue))
            {
                Shell.Current.DisplayAlert("Error",
                    "Please enter a whole number of pieces or loads.", "OK");
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

        // Removes one line from the cart.
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

        // ===== SAVING =====

        // Handles three cases: creating a new transaction, saving edits to
        // an existing one, and confirming payment.
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

            // Cash validation only applies when money is being taken now.
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
                finalAmountPaid = AmountPaid;
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

            // --- Existing transaction: update items and payment only ---
            if (_existingTransactionId != 0)
            {
                var existing = await _context.Transactions
                    .Include(t => t.Items)
                    .FirstOrDefaultAsync(t => t.TransactionId == _existingTransactionId);

                if (existing == null) return;

                existing.AmountPaid = finalAmountPaid;
                if (CollectingPayment)
                    existing.PaymentType = SelectedPaymentType;

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

                // Note: Status is NOT changed here. Only the Complete button
                // on the list page can finalise a transaction.
                await _context.SaveChangesAsync();
                await Shell.Current.GoToAsync("..");
                return;
            }

            // --- Brand new transaction ---
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

            // Update the customer's last-transaction date on the tracked copy.
            var trackedCustomer = await _context.Customers.FindAsync(SelectedCustomer.CustomerId);
            if (trackedCustomer != null)
                trackedCustomer.LastTransaction = DateTime.Now;

            await _context.SaveChangesAsync();
            await Shell.Current.GoToAsync("..");
        }

        // ===== OTHER NAVIGATION =====

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