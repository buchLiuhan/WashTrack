using WashTrack.MVVM.ViewModels;
namespace WashTrack.MVVM.Views
{
    public partial class ServiceDetailPage : ContentPage
    {
        private readonly ServiceDetailViewModel _viewModel;

        public ServiceDetailPage(ServiceDetailViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;
        }

        // Loads the inventory list for the supply pickers.
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadInventoryAsync();
        }
    }
}