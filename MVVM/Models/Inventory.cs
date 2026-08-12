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

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public DateTime? LastRestockedAt { get; set; }
    }
}