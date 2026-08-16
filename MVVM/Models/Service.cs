using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace WashTrack.Models
{
    public class Service
    {
        [Key]
        public int ServiceId { get; set; }

        [Required]
        public string ServiceName { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        // Mode A: Per Kilo
        public decimal? PricePerKilo { get; set; }

        // Mode B: Minimum Charge + Extra per Kilo
        public decimal? MinKilo { get; set; }
        public decimal? MinKiloCharge { get; set; }
        public decimal? ExcessPerKilo { get; set; }

        // Mode C: Flat Rate (per piece / per load)
        public decimal? FlatRate { get; set; }

        // ===== SUPPLY LINKS =====
        // Optional. Each slot links an inventory item and how much of it
        // one unit of this service consumes. "One unit" means 1kg for
        // weight-based services, or 1 piece/load for flat-rate services.
        public int? DetergentItemId { get; set; }
        public decimal? DetergentUsage { get; set; }

        public int? ConditionerItemId { get; set; }
        public decimal? ConditionerUsage { get; set; }

        public int? OtherItemId { get; set; }
        public decimal? OtherUsage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("DetergentItemId")]
        public Inventory? DetergentItem { get; set; }

        [ForeignKey("ConditionerItemId")]
        public Inventory? ConditionerItem { get; set; }

        [ForeignKey("OtherItemId")]
        public Inventory? OtherItem { get; set; }
    }
}