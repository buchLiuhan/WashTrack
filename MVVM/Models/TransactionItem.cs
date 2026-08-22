using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WashTrack.Models
{
    public class TransactionItem
    {
        [Key]
        public int TransactionItemId { get; set; }

        [Required]
        public int TransactionId { get; set; }

        [Required]
        public int ServiceId { get; set; }

        [Required]
        public decimal WeightKg { get; set; }

        [Required]
        public decimal LineCost { get; set; }

        [ForeignKey("TransactionId")]
        public Transaction? Transaction { get; set; }

        [ForeignKey("ServiceId")]
        public Service? Service { get; set; }

        // WeightKg holds kilos for weight-based services but a plain count
        // for flat-rate ones, so the unit belongs to the service, not to the
        // field. Flat-rate lines get no suffix — the service name already
        // says what's being counted ("Comforter — 3").
        [NotMapped]
        public string QuantityText => Service?.FlatRate.HasValue == true
            ? $"{WeightKg:F0}"
            : $"{WeightKg}kg";
    }
}