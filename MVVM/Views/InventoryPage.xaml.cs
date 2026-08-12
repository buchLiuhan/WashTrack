using WashTrack.MVVM.ViewModels;

namespace WashTrack.MVVM.Views
{
    public partial class InventoryPage : ContentPage
    {
        private readonly InventoryViewModel _viewModel;

        public InventoryPage(InventoryViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadInventoryAsync();
        }
    }
}