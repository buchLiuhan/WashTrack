using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WashTrack.Models
{
    public class InventoryUsageHistory
    {
        [Key]
        public int UsageId { get; set; }

        [Required]
        public int InventoryId { get; set; }

        [Required]
        public decimal QuantityUsed { get; set; }

        public DateTime UsageDate { get; set; } = DateTime.Now;

        public int? TransactionId { get; set; }

        public string? Notes { get; set; }

        // Navigation properties
        [ForeignKey("InventoryId")]
        public Inventory? Inventory { get; set; }

        [ForeignKey("TransactionId")]
        public Transaction? Transaction { get; set; }
    }
}