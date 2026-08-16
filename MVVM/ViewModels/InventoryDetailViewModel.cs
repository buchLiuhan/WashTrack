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
                Title = "Edit Item";
                ItemName = value.ItemName;
                CurrentStock = value.CurrentStock.ToString();
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

            if (!decimal.TryParse(CurrentStock, out decimal stock) || stock < 0)
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

            if (_isEditing)
            {
                // Fetch tracked so the update actually saves.
                var tracked = await _context.Inventories.FindAsync(InventoryItem.InventoryId);
                if (tracked == null) return;

                // Capture the old stock BEFORE overwriting it, otherwise the
                // restock comparison below always fails.
                decimal previousStock = tracked.CurrentStock;

                tracked.ItemName = ItemName;
                tracked.CurrentStock = stock;
                tracked.Unit = Unit;
                tracked.MinimumThreshold = threshold;
                tracked.ReorderQuantity = reorder > 0 ? reorder : null;
                tracked.UpdatedAt = DateTime.Now;

                // Stock going up means the owner restocked.
                if (stock > previousStock)
                    tracked.LastRestockedAt = DateTime.Now;
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