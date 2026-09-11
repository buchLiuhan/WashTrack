using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.ObjectModel;
namespace WashTrack.Models
{
    // One laundry job: a customer, the services they ordered, and how far along
    // the job is. Tracked with three independent states — Status (the job as a
    // whole), WashStatus (what the staff are physically doing), and payment.
    public class Transaction
    {
        [Key]
        public int TransactionId { get; set; }

        [Required]
        public int CustomerId { get; set; }

        // Sum of every TransactionItem's subtotal, computed at checkout.
        [Required]
        public decimal TotalCost { get; set; }

        // "Pending" or "Completed". Drives IsPendingConverter / IsCompletedConverter.
        [Required]
        public string Status { get; set; } = "Pending";

        // How the customer gets the laundry: "Pickup" or "Delivery".
        [Required]
        public string FulfillmentType { get; set; } = "Pickup";

        // "Pay Now" (paid at drop-off) or "Pay Later" (settled on release).
        [Required]
        public string PaymentType { get; set; } = "Pay Now";

        // Null until money is collected; partial payments are allowed.
        public decimal? AmountPaid { get; set; }

        // Laundry stage: "To Be Washed" -> "Washing" -> "Washed".
        // Moving to "Washing" is what deducts supplies from inventory.
        // Separate from Status so staff can track progress before the job closes.
        [Required]
        public string WashStatus { get; set; } = "To Be Washed";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Set only when Status flips to "Completed". Used by the sales reports.
        public DateTime? CompletedAt { get; set; }

        [ForeignKey("CustomerId")]
        public Customer? Customer { get; set; }

        // The individual services on this job. ObservableCollection so the
        // detail page updates as items are added or removed.
        public ObservableCollection<TransactionItem> Items { get; set; } = new();

        // Convenience flag for the UI — not a database column.
        [NotMapped]
        public bool IsPaid => (AmountPaid ?? 0) >= TotalCost;
    }
}