using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.ObjectModel;
namespace WashTrack.Models
{
    public class Transaction
    {
        [Key]
        public int TransactionId { get; set; }
        [Required]
        public int CustomerId { get; set; }
        [Required]
        public decimal TotalCost { get; set; }
        [Required]
        public string Status { get; set; } = "Pending";
        [Required]
        public string FulfillmentType { get; set; } = "Pickup";
        [Required]
        public string PaymentType { get; set; } = "Pay Now";
        public decimal? AmountPaid { get; set; }

        [Required]
        public string WashStatus { get; set; } = "To Be Washed";

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? CompletedAt { get; set; }
        [ForeignKey("CustomerId")]
        public Customer? Customer { get; set; }
        public ObservableCollection<TransactionItem> Items { get; set; } = new();

        [NotMapped]
        public bool IsPaid => (AmountPaid ?? 0) >= TotalCost;
    }
}