using WashTrack.MVVM.ViewModels;

namespace WashTrack.MVVM.Views
{
    public partial class ReportsPage : ContentPage
    {
        private readonly ReportsViewModel _viewModel;

        public ReportsPage(ReportsViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadReportsAsync();
        }
    }
}