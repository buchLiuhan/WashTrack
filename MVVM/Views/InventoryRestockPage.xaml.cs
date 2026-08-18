using WashTrack.MVVM.ViewModels;

namespace WashTrack.MVVM.Views
{
    public partial class InventoryRestockPage : ContentPage
    {
        public InventoryRestockPage(InventoryRestockViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
