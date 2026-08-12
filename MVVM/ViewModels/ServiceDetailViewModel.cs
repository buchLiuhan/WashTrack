using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

        private bool _isEditing = false;

        public bool IsPerKiloMode => SelectedPricingMode == "Per Kilo";
        public bool IsMinChargeMode => SelectedPricingMode == "Minimum Charge";
        public bool IsFlatRateMode => SelectedPricingMode == "Flat Rate";

        public ServiceDetailViewModel(WashTrackContext context)
        {
            _context = context;
        }

        // Fires automatically whenever the Picker's selection changes.
        // Tells the 3 IsXMode properties to refresh, which is what
        // drives the IsEnabled bindings on the XAML side.
        partial void OnSelectedPricingModeChanged(string value)
        {
            OnPropertyChanged(nameof(IsPerKiloMode));
            OnPropertyChanged(nameof(IsMinChargeMode));
            OnPropertyChanged(nameof(IsFlatRateMode));
        }

        partial void OnServiceChanged(Service value)
        {
            if (value != null && value.ServiceId != 0)
            {
                _isEditing = true;
                Title = "Edit Service";
                ServiceName = value.ServiceName;

                // Figure out which mode this service was saved as,
                // based on which fields actually have values.
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
            }
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

            if (_isEditing)
            {
                Service.ServiceName = ServiceName;
                Service.PricePerKilo = finalPricePerKilo;
                Service.MinKilo = finalMinKilo;
                Service.MinKiloCharge = finalMinKiloCharge;
                Service.ExcessPerKilo = finalExcessPerKilo;
                Service.FlatRate = finalFlatRate;
                _context.Services.Update(Service);
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