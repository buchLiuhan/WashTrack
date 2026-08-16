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

        [Required]
        public decimal MinimumThreshold { get; set; }

        public decimal? ReorderQuantity { get; set; }

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