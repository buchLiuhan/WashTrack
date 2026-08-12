using System.ComponentModel.DataAnnotations;

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

        // Mode C: Flat Rate (per piece)
        public decimal? FlatRate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}