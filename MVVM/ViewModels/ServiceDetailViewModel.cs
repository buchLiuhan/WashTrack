using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using WashTrack.Data;
using WashTrack.Models;

namespace WashTrack.MVVM.ViewModels
{
    [QueryProperty(nameof(Service), "Service")]
    public partial class ServiceDetailViewModel : ObservableObject
    {
        private readonly WashTrackContext _context;

        [ObservableProperty]
        private Service service = new();

        [ObservableProperty]
        private string title = "Add Service";

        [ObservableProperty]
        private string serviceName = string.Empty;

        [ObservableProperty]
        private ObservableCollection<string> pricingModes =
            new() { "Per Kilo", "Minimum Charge", "Flat Rate" };

        [ObservableProperty]
        private string selectedPricingMode = "Per Kilo";

        [ObservableProperty]
        private string pricePerKilo = string.Empty;

        [ObservableProperty]
        private string minKilo = string.Empty;

        [ObservableProperty]
        private string minKiloCharge = string.Empty;

        [ObservableProperty]
        private string excessPerKilo = string.Empty;

        [ObservableProperty]
        private string flatRate = string.Empty;

        // ===== SUPPLY LINKING =====

        [ObservableProperty]
        private ObservableCollection<Inventory> availableItems = new();

        [ObservableProperty]
        private bool hasInventoryItems;

        [ObservableProperty] private Inventory? selectedDetergentItem;
        [ObservableProperty] private string detergentUsage = string.Empty;
        [ObservableProperty] private bool hasDetergentSelection;

        [ObservableProperty] private Inventory? selectedConditionerItem;
        [ObservableProperty] private string conditionerUsage = string.Empty;
        [ObservableProperty] private bool hasConditionerSelection;

        [ObservableProperty] private Inventory? selectedOtherItem;
        [ObservableProperty] private string otherUsage = string.Empty;
        [ObservableProperty] private bool hasOtherSelection;

        private bool _isEditing = false;

        public bool IsPerKiloMode => SelectedPricingMode == "Per Kilo";
        public bool IsMinChargeMode => SelectedPricingMode == "Minimum Charge";
        public bool IsFlatRateMode => SelectedPricingMode == "Flat Rate";

        // Supply rates mean different things depending on how the service
        // is priced: per kilo for weight-based, per piece/load for flat rate.
        public string UsageUnitLabel => IsFlatRateMode ? "per piece/load" : "per kg";

        public string UsageHelpText => IsFlatRateMode
            ? "Amount used for ONE piece or load."
            : "Amount used for ONE kilo of laundry.";

        public ServiceDetailViewModel(WashTrackContext context)
        {
            _context = context;
        }

        // Refreshes the mode-dependent visibility and labels.
        partial void OnSelectedPricingModeChanged(string value)
        {
            OnPropertyChanged(nameof(IsPerKiloMode));
            OnPropertyChanged(nameof(IsMinChargeMode));
            OnPropertyChanged(nameof(IsFlatRateMode));
            OnPropertyChanged(nameof(UsageUnitLabel));
            OnPropertyChanged(nameof(UsageHelpText));
        }

        partial void OnSelectedDetergentItemChanged(Inventory? value) => HasDetergentSelection = value != null;
        partial void OnSelectedConditionerItemChanged(Inventory? value) => HasConditionerSelection = value != null;
        partial void OnSelectedOtherItemChanged(Inventory? value) => HasOtherSelection = value != null;

        // ===== CLEARING A SLOT =====

        [RelayCommand]
        public void ClearDetergent()
        {
            SelectedDetergentItem = null;
            DetergentUsage = string.Empty;
        }

        [RelayCommand]
        public void ClearConditioner()
        {
            SelectedConditionerItem = null;
            ConditionerUsage = string.Empty;
        }

        [RelayCommand]
        public void ClearOther()
        {
            SelectedOtherItem = null;
            OtherUsage = string.Empty;
        }

        // ===== LOADING =====

        // Fills the form when an existing service is passed in.
        partial void OnServiceChanged(Service value)
        {
            if (value != null && value.ServiceId != 0)
            {
                _isEditing = true;
                Title = "Edit Service";
                ServiceName = value.ServiceName;

                // Work out which pricing mode this was saved as.
                if (value.FlatRate.HasValue)
                {
                    SelectedPricingMode = "Flat Rate";
                    FlatRate = value.FlatRate.Value.ToString();
                }
                else if (value.MinKilo.HasValue)
                {
                    SelectedPricingMode = "Minimum Charge";
                    MinKilo = value.MinKilo.Value.ToString();
                    MinKiloCharge = value.MinKiloCharge?.ToString() ?? string.Empty;
                    ExcessPerKilo = value.ExcessPerKilo?.ToString() ?? string.Empty;
                }
                else if (value.PricePerKilo.HasValue)
                {
                    SelectedPricingMode = "Per Kilo";
                    PricePerKilo = value.PricePerKilo.Value.ToString();
                }

                DetergentUsage = value.DetergentUsage?.ToString() ?? string.Empty;
                ConditionerUsage = value.ConditionerUsage?.ToString() ?? string.Empty;
                OtherUsage = value.OtherUsage?.ToString() ?? string.Empty;
            }
        }

        // Loads the inventory list for the three supply pickers.
        // Called from OnAppearing so the list is ready before binding.
        [RelayCommand]
        public async Task LoadInventoryAsync()
        {
            var items = await _context.Inventories
                .AsNoTracking()
                .Where(i => i.IsActive)
                .OrderBy(i => i.ItemName)
                .ToListAsync();

            AvailableItems = new ObservableCollection<Inventory>(items);
            HasInventoryItems = items.Count > 0;

            // Re-select the previously linked items when editing.
            if (_isEditing)
            {
                if (Service.DetergentItemId.HasValue)
                    SelectedDetergentItem = AvailableItems
                        .FirstOrDefault(i => i.InventoryId == Service.DetergentItemId.Value);

                if (Service.ConditionerItemId.HasValue)
                    SelectedConditionerItem = AvailableItems
                        .FirstOrDefault(i => i.InventoryId == Service.ConditionerItemId.Value);

                if (Service.OtherItemId.HasValue)
                    SelectedOtherItem = AvailableItems
                        .FirstOrDefault(i => i.InventoryId == Service.OtherItemId.Value);
            }
        }

        [RelayCommand]
        public async Task GoToInventoryAsync()
        {
            await Shell.Current.GoToAsync("///InventoryPage");
        }

        // ===== SAVING =====

        // Validates one supply slot. Returns false if the entry is invalid.
        private bool TryParseUsage(Inventory? item, string input, string slotName, out decimal? result)
        {
            result = null;
            if (item == null) return true;   // slot unused, nothing to validate

            if (!decimal.TryParse(input, out decimal val) || val <= 0)
            {
                Shell.Current.DisplayAlert("Error",
                    $"Please enter a valid {slotName} usage amount.", "OK");
                return false;
            }

            result = val;
            return true;
        }

        [RelayCommand]
        public async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(ServiceName))
            {
                await Shell.Current.DisplayAlert("Error", "Service name is required.", "OK");
                return;
            }

            decimal? finalPricePerKilo = null;
            decimal? finalMinKilo = null;
            decimal? finalMinKiloCharge = null;
            decimal? finalExcessPerKilo = null;
            decimal? finalFlatRate = null;

            if (SelectedPricingMode == "Per Kilo")
            {
                if (!decimal.TryParse(PricePerKilo, out decimal priceVal) || priceVal <= 0)
                {
                    await Shell.Current.DisplayAlert("Error", "Please enter a valid price per kilo.", "OK");
                    return;
                }
                finalPricePerKilo = priceVal;
            }
            else if (SelectedPricingMode == "Minimum Charge")
            {
                if (!decimal.TryParse(MinKilo, out decimal minKiloVal) || minKiloVal <= 0)
                {
                    await Shell.Current.DisplayAlert("Error", "Please enter a valid minimum weight.", "OK");
                    return;
                }
                if (!decimal.TryParse(MinKiloCharge, out decimal minChargeVal) || minChargeVal <= 0)
                {
                    await Shell.Current.DisplayAlert("Error", "Please enter a valid minimum charge.", "OK");
                    return;
                }
                finalMinKilo = minKiloVal;
                finalMinKiloCharge = minChargeVal;

                // Optional — only validate if the owner actually typed something
                if (!string.IsNullOrWhiteSpace(ExcessPerKilo))
                {
                    if (!decimal.TryParse(ExcessPerKilo, out decimal excessVal) || excessVal <= 0)
                    {
                        await Shell.Current.DisplayAlert("Error", "Please enter a valid excess charge per kilo.", "OK");
                        return;
                    }
                    finalExcessPerKilo = excessVal;
                }
            }
            else // Flat Rate
            {
                if (!decimal.TryParse(FlatRate, out decimal flatVal) || flatVal <= 0)
                {
                    await Shell.Current.DisplayAlert("Error", "Please enter a valid flat rate.", "OK");
                    return;
                }
                finalFlatRate = flatVal;
            }

            // Validate the three optional supply slots.
            if (!TryParseUsage(SelectedDetergentItem, DetergentUsage, "detergent", out decimal? dUsage)) return;
            if (!TryParseUsage(SelectedConditionerItem, ConditionerUsage, "conditioner", out decimal? cUsage)) return;
            if (!TryParseUsage(SelectedOtherItem, OtherUsage, "other supply", out decimal? oUsage)) return;

            if (_isEditing)
            {
                var tracked = await _context.Services.FindAsync(Service.ServiceId);
                if (tracked == null) return;

                tracked.ServiceName = ServiceName;
                tracked.PricePerKilo = finalPricePerKilo;
                tracked.MinKilo = finalMinKilo;
                tracked.MinKiloCharge = finalMinKiloCharge;
                tracked.ExcessPerKilo = finalExcessPerKilo;
                tracked.FlatRate = finalFlatRate;

                tracked.DetergentItemId = SelectedDetergentItem?.InventoryId;
                tracked.DetergentUsage = dUsage;
                tracked.ConditionerItemId = SelectedConditionerItem?.InventoryId;
                tracked.ConditionerUsage = cUsage;
                tracked.OtherItemId = SelectedOtherItem?.InventoryId;
                tracked.OtherUsage = oUsage;
            }
            else
            {
                await _context.Services.AddAsync(new Service
                {
                    ServiceName = ServiceName,
                    PricePerKilo = finalPricePerKilo,
                    MinKilo = finalMinKilo,
                    MinKiloCharge = finalMinKiloCharge,
                    ExcessPerKilo = finalExcessPerKilo,
                    FlatRate = finalFlatRate,
                    DetergentItemId = SelectedDetergentItem?.InventoryId,
                    DetergentUsage = dUsage,
                    ConditionerItemId = SelectedConditionerItem?.InventoryId,
                    ConditionerUsage = cUsage,
                    OtherItemId = SelectedOtherItem?.InventoryId,
                    OtherUsage = oUsage,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
            await Shell.Current.GoToAsync("..");
        }

        [RelayCommand]
        public async Task CancelAsync()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}