using Microsoft.Extensions.DependencyInjection;

namespace WashTrack
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(MVVM.Views.ServiceDetailPage), typeof(MVVM.Views.ServiceDetailPage));
            Routing.RegisterRoute(nameof(MVVM.Views.CustomerDetailPage), typeof(MVVM.Views.CustomerDetailPage));
            Routing.RegisterRoute(nameof(MVVM.Views.TransactionDetailPage), typeof(MVVM.Views.TransactionDetailPage));
            Routing.RegisterRoute(nameof(MVVM.Views.InventoryDetailPage), typeof(MVVM.Views.InventoryDetailPage));
            Routing.RegisterRoute(nameof(MVVM.Views.InventoryRestockPage), typeof(MVVM.Views.InventoryRestockPage));
            Routing.RegisterRoute(nameof(MVVM.Views.ReportsPage), typeof(MVVM.Views.ReportsPage));
        }

        // Mirrors LoginViewModel.CompleteLogin in reverse: swapping the
        // window's page tears this Shell down, so the Android back button
        // cannot walk back into the app afterwards. LoginPage is resolved
        // from DI rather than newed up — it needs its ViewModel injected,
        // the same way App.CreateWindow builds it at startup.
        private async void OnLogoutTapped(object? sender, TappedEventArgs e)
        {
            bool confirmed = await DisplayAlert(
                "Log Out",
                "Log out of WashTrack?",
                "Log Out", "Cancel");

            if (!confirmed) return;

            FlyoutIsPresented = false;

            var services = Application.Current?.Handler?.MauiContext?.Services;
            if (services == null) return;

            var loginPage = services.GetRequiredService<MVVM.Views.LoginPage>();
            Application.Current!.Windows[0].Page = loginPage;
        }
    }
}