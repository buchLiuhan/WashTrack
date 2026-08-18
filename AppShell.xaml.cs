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
    }
}