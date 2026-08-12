using WashTrack.MVVM.ViewModels;

namespace WashTrack.MVVM.Views
{
    public partial class CustomersPage : ContentPage
    {
        private readonly CustomersViewModel _viewModel;

        public CustomersPage(CustomersViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadCustomersAsync();
        }
    }
}