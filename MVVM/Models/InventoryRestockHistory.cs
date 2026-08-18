using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WashTrack.Models
{
    public class InventoryRestockHistory
    {
        [Key]
        public int RestockId { get; set; }

        [Required]
        public int InventoryId { get; set; }

        // Positive = new supply received. Negative = correction (spillage,
        // spoilage, miscount) — same entry point, distinguished by sign.
        [Required]
        public decimal QuantityChange { get; set; }

        public DateTime RestockDate { get; set; } = DateTime.Now;

        public string? Notes { get; set; }

        [ForeignKey("InventoryId")]
        public Inventory? Inventory { get; set; }
    }
}
