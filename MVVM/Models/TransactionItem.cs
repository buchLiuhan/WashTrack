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
    }
}