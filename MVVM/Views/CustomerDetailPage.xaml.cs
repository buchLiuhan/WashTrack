using WashTrack.MVVM.ViewModels;

namespace WashTrack.MVVM.Views
{
    public partial class CustomerDetailPage : ContentPage
    {
        public CustomerDetailPage(CustomerDetailViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}