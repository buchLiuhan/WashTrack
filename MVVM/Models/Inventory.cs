using System.ComponentModel.DataAnnotations;
namespace WashTrack.Models
{
    public class Inventory
    {
        [Key]
        public int InventoryId { get; set; }

        [Required]
        public string ItemName { get; set; } = string.Empty;

        [Required]
        public decimal CurrentStock { get; set; }

        [Required]
        public string Unit { get; set; } = string.Empty;

        // WHEN to reorder: stock at or below this raises the low-stock alert.
        [Required]
        public decimal MinimumThreshold { get; set; }

        // HOW MUCH is normally added when topping this item up — a bought
        // pack size or a home-made batch, the app doesn't care which.
        // Optional, set by the owner, and it never affects the low-stock
        // alert (that's MinimumThreshold alone). Used to prefill the restock
        // quantity and to fill out the "Restock now" hint.
        public decimal? UsualRestockAmount { get; set; }

        public decimal? UnitCost { get; set; }

        // Soft delete: deactivated items keep their usage history so past
        // inventory reports stay accurate.
        public bool IsActive { get; set; } = true;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public DateTime? LastRestockedAt { get; set; }

        // True when stock has fallen to or below the alert threshold.
        // NotMapped: calculated, not stored.
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public bool IsLowStock => CurrentStock <= MinimumThreshold;
    }
}