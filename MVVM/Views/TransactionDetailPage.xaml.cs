using WashTrack.MVVM.ViewModels;

namespace WashTrack.MVVM.Views
{
    public partial class TransactionDetailPage : ContentPage
    {
        private readonly TransactionDetailViewModel _viewModel;

        public TransactionDetailPage(TransactionDetailViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadDataAsync();
        }
    }
}