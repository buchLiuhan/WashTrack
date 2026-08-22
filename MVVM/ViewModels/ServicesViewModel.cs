using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using WashTrack.Data;
using WashTrack.Models;
using WashTrack.MVVM.Views;

namespace WashTrack.MVVM.ViewModels
{
    public partial class ServicesViewModel : ObservableObject
    {
        private readonly WashTrackContext _context;

        [ObservableProperty]
        private ObservableCollection<Service> services = new();

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private bool showingInactive = false;

        [ObservableProperty]
        private string toggleButtonText = "Show Inactive";

        public ServicesViewModel(WashTrackContext context)
        {
            _context = context;
        }

        [RelayCommand]
        public async Task LoadServicesAsync()
        {
            IsLoading = true;
            var list = await _context.Services
                .AsNoTracking()
                .Where(s => s.IsActive != ShowingInactive)
                .OrderBy(s => s.ServiceName)
                .ToListAsync();
            Services = new ObservableCollection<Service>(list);
            IsLoading = false;
        }

        [RelayCommand]
        public async Task ToggleInactiveAsync()
        {
            ShowingInactive = !ShowingInactive;
            ToggleButtonText = ShowingInactive ? "Show Active" : "Show Inactive";
            await LoadServicesAsync();
        }

        [RelayCommand]
        public async Task AddServiceAsync()
        {
            await Shell.Current.GoToAsync(nameof(ServiceDetailPage));
        }

        [RelayCommand]
        public async Task EditServiceAsync(Service service)
        {
            var parameters = new Dictionary<string, object>
            {
                { "Service", service }
            };
            await Shell.Current.GoToAsync(nameof(ServiceDetailPage), parameters);
        }

        [RelayCommand]
        public async Task DeleteServiceAsync(Service service)
        {
            var hasPending = await _context.Transactions
               .AsNoTracking()
               .Where(t => t.Status == "Pending")
               .AnyAsync(t => t.Items.Any(i => i.ServiceId == service.ServiceId));

            if (hasPending)
            {
                await Shell.Current.DisplayAlert(
                    "Cannot Deactivate",
                    $"'{service.ServiceName}' has pending orders. Complete or cancel them first before deactivating this service.",
                    "OK");
                return;
            }

            bool confirm = await Shell.Current.DisplayAlert(
                "Deactivate Service",
                $"Deactivate '{service.ServiceName}'? It won't appear in new transactions.",
                "Yes", "No");

            if (!confirm) return;

            // Re-fetch instead of Update(service): the list is loaded
            // AsNoTracking, so `service` is detached, and attaching it would
            // throw once this same row has already been tracked earlier in
            // the page's lifetime. FindAsync returns the tracked instance
            // when one exists, so no duplicate is ever attached.
            var tracked = await _context.Services.FindAsync(service.ServiceId);
            if (tracked == null) return;

            tracked.IsActive = false;
            await _context.SaveChangesAsync();
            await LoadServicesAsync();
        }

        [RelayCommand]
        public async Task RestoreServiceAsync(Service service)
        {
            var tracked = await _context.Services.FindAsync(service.ServiceId);
            if (tracked == null) return;

            tracked.IsActive = true;
            await _context.SaveChangesAsync();
            await LoadServicesAsync();
        }

        [RelayCommand]
        public async Task PermanentDeleteServiceAsync(Service service)
        {
            var hasAnyTransactions = await _context.TransactionItems
               .AsNoTracking()
               .AnyAsync(i => i.ServiceId == service.ServiceId);

            if (hasAnyTransactions)
            {
                await Shell.Current.DisplayAlert(
                    "Cannot Delete",
                    $"'{service.ServiceName}' has transaction history and cannot be permanently deleted, since that would break past sales records. It will stay hidden as inactive instead.",
                    "OK");
                return;
            }

            bool confirm = await Shell.Current.DisplayAlert(
                "Permanently Delete",
                $"Permanently delete '{service.ServiceName}'? This cannot be undone.",
                "Delete Forever", "Cancel");

            if (!confirm) return;

            var tracked = await _context.Services.FindAsync(service.ServiceId);
            if (tracked == null) return;

            _context.Services.Remove(tracked);
            await _context.SaveChangesAsync();
            await LoadServicesAsync();
        }
    }
}