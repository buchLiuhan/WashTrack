using System.ComponentModel.DataAnnotations;

namespace WashTrack.Models
{
    // A walk-in or regular customer of the shop.
    public class Customer
    {
        [Key]
        public int CustomerId { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string ContactNumber { get; set; } = string.Empty;

        public string? Email { get; set; }

        [Required]
        public string Address { get; set; } = string.Empty;

        // Soft delete. Customers are deactivated rather than removed so their
        // past transactions keep a valid reference for reporting.
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Cached date of this customer's most recent transaction, so the
        // customer list can sort/display it without querying every job.
        public DateTime? LastTransaction { get; set; }
    }
}