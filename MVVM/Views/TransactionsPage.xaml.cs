using WashTrack.MVVM.ViewModels;

namespace WashTrack.MVVM.Views
{
    public partial class TransactionsPage : ContentPage
    {
        private readonly TransactionsViewModel _viewModel;

        public TransactionsPage(TransactionsViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadTransactionsAsync();
        }
    }
}