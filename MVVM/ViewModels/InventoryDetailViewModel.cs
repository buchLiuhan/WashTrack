using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WashTrack.Data;
using WashTrack.Models;

namespace WashTrack.MVVM.ViewModels
{
    [QueryProperty(nameof(InventoryItem), "InventoryItem")]
    public partial class InventoryDetailViewModel : ObservableObject
    {
        private readonly WashTrackContext _context;

        [ObservableProperty]
        private Inventory inventoryItem = new();

        [ObservableProperty]
        private string title = "Add Item";

        [ObservableProperty]
        private string itemName = string.Empty;

        [ObservableProperty]
        private string currentStock = string.Empty;

        [ObservableProperty]
        private string currentStockDisplay = string.Empty;

        [ObservableProperty]
        private bool isNewItem = true;

        [ObservableProperty]
        private string unit = "ml";

        [ObservableProperty]
        private string minimumThreshold = string.Empty;

        [ObservableProperty]
        private string reorderQuantity = string.Empty;

        private bool _isEditing = false;

        public InventoryDetailViewModel(WashTrackContext context)
        {
            _context = context;
        }

        // Fills the form when an existing item is passed in.
        partial void OnInventoryItemChanged(Inventory value)
        {
            if (value != null && value.InventoryId != 0)
            {
                _isEditing = true;
                IsNewItem = false;
                Title = "Edit Item";
                ItemName = value.ItemName;
                CurrentStockDisplay = $"{value.CurrentStock:F0} {value.Unit}";
                Unit = value.Unit;
                MinimumThreshold = value.MinimumThreshold.ToString();
                ReorderQuantity = value.ReorderQuantity?.ToString() ?? string.Empty;
            }
        }

        // ===== SAVING =====

        [RelayCommand]
        public async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(ItemName))
            {
                await Shell.Current.DisplayAlert("Error", "Item name is required.", "OK");
                return;
            }

            decimal stock = 0;
            if (IsNewItem && (!decimal.TryParse(CurrentStock, out stock) || stock < 0))
            {
                await Shell.Current.DisplayAlert("Error", "Please enter a valid stock quantity.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(Unit))
            {
                await Shell.Current.DisplayAlert("Error", "Unit is required (e.g. ml, pcs).", "OK");
                return;
            }

            if (!decimal.TryParse(MinimumThreshold, out decimal threshold) || threshold < 0)
            {
                await Shell.Current.DisplayAlert("Error", "Please enter a valid minimum threshold.", "OK");
                return;
            }

            decimal.TryParse(ReorderQuantity, out decimal reorder);

            string reorderText = reorder > 0 ? $"{reorder:F0} {Unit}" : "Not set";
            string summary =
                $"Item: {ItemName}\n" +
                (IsNewItem ? $"Current Stock: {stock:F0} {Unit}\n" : string.Empty) +
                $"Low Stock Alert: {threshold:F0} {Unit}\n" +
                $"Reorder Quantity: {reorderText}";

            // Soft warning only — reorder quantity is informational and
            // nothing downstream enforces it, so this never blocks saving.
            if (reorder > 0 && reorder <= threshold)
            {
                summary += "\n\n⚠️ This reorder quantity won't clear the low-stock warning after one restock — you may want to increase it.";
            }

            bool confirmed = await Shell.Current.DisplayAlert(
                _isEditing ? "Confirm Changes" : "Confirm New Item",
                summary,
                "Confirm", "Cancel");
            if (!confirmed) return;

            if (_isEditing)
            {
                // Fetch tracked so the update actually saves.
                var tracked = await _context.Inventories.FindAsync(InventoryItem.InventoryId);
                if (tracked == null) return;

                // Stock is changed only via Restock, not here.
                tracked.ItemName = ItemName;
                tracked.Unit = Unit;
                tracked.MinimumThreshold = threshold;
                tracked.ReorderQuantity = reorder > 0 ? reorder : null;
                tracked.UpdatedAt = DateTime.Now;
            }
            else
            {
                var newItem = new Inventory
                {
                    ItemName = ItemName,
                    CurrentStock = stock,
                    Unit = Unit,
                    MinimumThreshold = threshold,
                    ReorderQuantity = reorder > 0 ? reorder : null,
                    IsActive = true,
                    UpdatedAt = DateTime.Now,
                    LastRestockedAt = DateTime.Now
                };
                await _context.Inventories.AddAsync(newItem);
            }

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