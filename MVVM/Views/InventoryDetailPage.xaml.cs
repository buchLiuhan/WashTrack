using WashTrack.MVVM.ViewModels;

namespace WashTrack.MVVM.Views
{
    public partial class InventoryDetailPage : ContentPage
    {
        public InventoryDetailPage(InventoryDetailViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}