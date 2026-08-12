using WashTrack.MVVM.ViewModels;

namespace WashTrack.MVVM.Views
{
    public partial class ServicesPage : ContentPage
    {
        private readonly ServicesViewModel _viewModel;

        public ServicesPage(ServicesViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadServicesAsync();
        }
    }
}