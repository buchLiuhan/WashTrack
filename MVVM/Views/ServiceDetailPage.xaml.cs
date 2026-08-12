using WashTrack.MVVM.ViewModels;

namespace WashTrack.MVVM.Views
{
    public partial class ServiceDetailPage : ContentPage
    {
        public ServiceDetailPage(ServiceDetailViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}