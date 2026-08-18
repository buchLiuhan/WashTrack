using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WashTrack.Data;
using WashTrack.Models;

namespace WashTrack.MVVM.ViewModels
{
    [QueryProperty(nameof(InventoryItem), "InventoryItem")]
    public partial class InventoryRestockViewModel : ObservableObject
    {
        private readonly WashTrackContext _context;

        [ObservableProperty]
        private Inventory inventoryItem = new();

        [ObservableProperty]
        private string itemName = string.Empty;

        [ObservableProperty]
        private string currentStockText = string.Empty;

        [ObservableProperty]
        private string quantityChange = string.Empty;

        [ObservableProperty]
        private string notes = string.Empty;

        public InventoryRestockViewModel(WashTrackContext context)
        {
            _context = context;
        }

        partial void OnInventoryItemChanged(Inventory value)
        {
            if (value == null) return;
            ItemName = value.ItemName;
            CurrentStockText = $"{value.CurrentStock:F0} {value.Unit}";
        }

        [RelayCommand]
        public async Task SaveAsync()
        {
            if (!decimal.TryParse(QuantityChange, out decimal quantity) || quantity == 0)
            {
                await Shell.Current.DisplayAlert("Error", "Enter a nonzero quantity. Use a positive number for new supply, negative for corrections (spillage, spoilage, miscount).", "OK");
                return;
            }

            // Corrections must explain themselves, since they don't come from
            // a transaction and would otherwise look like unexplained loss.
            if (quantity < 0 && string.IsNullOrWhiteSpace(Notes))
            {
                await Shell.Current.DisplayAlert("Error", "A note is required when reducing stock (e.g. spillage, spoilage, miscount).", "OK");
                return;
            }

            var tracked = await _context.Inventories.FindAsync(InventoryItem.InventoryId);
            if (tracked == null) return;

            decimal newStock = tracked.CurrentStock + quantity;
            if (newStock < 0)
            {
                await Shell.Current.DisplayAlert("Error", $"This would bring stock below zero (currently {tracked.CurrentStock:F0} {tracked.Unit}).", "OK");
                return;
            }

            string direction = quantity > 0 ? "Add" : "Remove";
            bool confirmed = await Shell.Current.DisplayAlert(
                "Confirm",
                $"{direction} {Math.Abs(quantity):F0} {tracked.Unit} {(quantity > 0 ? "to" : "from")} '{tracked.ItemName}'?\nNew stock: {newStock:F0} {tracked.Unit}",
                "Confirm", "Cancel");
            if (!confirmed) return;

            tracked.CurrentStock = newStock;
            if (quantity > 0)
                tracked.LastRestockedAt = DateTime.Now;
            tracked.UpdatedAt = DateTime.Now;

            await _context.InventoryRestockHistories.AddAsync(new InventoryRestockHistory
            {
                InventoryId = tracked.InventoryId,
                QuantityChange = quantity,
                RestockDate = DateTime.Now,
                Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes
            });

            await _context.SaveChangesAsync();
            await Shell.Current.GoToAsync("..");
        }

        [RelayCommand]
        public async Task CancelAsync()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
