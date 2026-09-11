using Microsoft.EntityFrameworkCore;
using WashTrack.Models;
namespace WashTrack.Data
{
    // EF Core database context — the single gateway to the SQLite file that
    // stores everything WashTrack knows. Registered as Transient in
    // MauiProgram so each page gets its own short-lived context.
    public class WashTrackContext : DbContext
    {
        public WashTrackContext(DbContextOptions<WashTrackContext> options) : base(options) { }

        // One DbSet per table.

        public DbSet<User> Users { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<TransactionItem> TransactionItems { get; set; }
        public DbSet<Inventory> Inventories { get; set; }
        public DbSet<InventoryUsageHistory> InventoryUsageHistories { get; set; }
        public DbSet<InventoryRestockHistory> InventoryRestockHistories { get; set; }

        // Decides where the database file lives. MauiProgram registers this
        // context without a connection string, so at runtime this is the path
        // that's actually used — LocalApplicationData, which is per-user and
        // survives app updates. The IsConfigured guard leaves any externally
        // supplied options alone (that's how the design-time factory overrides it).
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                string dbPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "washtrack.db");
                optionsBuilder.UseSqlite($"Data Source={dbPath}");
            }
        }

        // Relationship rules. The OnDelete choice is deliberate in each case
        // and follows one principle: sales history must stay intact and
        // readable forever, so nothing a report depends on can be deleted
        // out from under it. Three behaviors are used —
        //   Restrict — block the delete; the parent must be deactivated instead
        //   Cascade  — child rows are meaningless alone, so remove them too
        //   SetNull  — the link is optional; drop it and keep the row
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Restrict: a customer with transactions can't be deleted, or their
            // past sales would lose the name attached to them. CustomersViewModel
            // deactivates via Customer.IsActive instead.
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Customer)
                .WithMany()
                .HasForeignKey(t => t.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Cascade: line items belong to their transaction and mean nothing
            // without it, so deleting the job clears its items automatically.
            modelBuilder.Entity<TransactionItem>()
                .HasOne(ti => ti.Transaction)
                .WithMany(t => t.Items)
                .HasForeignKey(ti => ti.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Restrict: a service that appears on any past job can't be deleted,
            // or that sale would lose what was actually sold. This is the rule
            // PermanentDeleteServiceAsync checks before offering deletion.
            modelBuilder.Entity<TransactionItem>()
                .HasOne(ti => ti.Service)
                .WithMany()
                .HasForeignKey(ti => ti.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            // Restrict on both sides: usage history is the audit trail for
            // where stock went. Deleting either the item or the job it was
            // consumed on would leave unexplained losses in the records.
            modelBuilder.Entity<InventoryUsageHistory>()
                .HasOne(i => i.Inventory)
                .WithMany()
                .HasForeignKey(i => i.InventoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InventoryUsageHistory>()
                .HasOne(i => i.Transaction)
                .WithMany()
                .HasForeignKey(i => i.TransactionId)
                .OnDelete(DeleteBehavior.Restrict);

            // Restrict: same reasoning for restocks — the record of supply
            // coming in has to outlive any edit to the item itself.
            modelBuilder.Entity<InventoryRestockHistory>()
                .HasOne(r => r.Inventory)
                .WithMany()
                .HasForeignKey(r => r.InventoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // ===== SUPPLY LINKS =====
            // SetNull: if an inventory item is ever permanently deleted,
            // the service simply loses the link instead of breaking.
            // Safe to null here — unlike the history tables above, these are
            // optional convenience links (see Models/Service.cs), so losing
            // one costs a future auto-deduction, not a past record.
            modelBuilder.Entity<Service>()
                .HasOne(s => s.DetergentItem)
                .WithMany()
                .HasForeignKey(s => s.DetergentItemId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Service>()
                .HasOne(s => s.ConditionerItem)
                .WithMany()
                .HasForeignKey(s => s.ConditionerItemId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Service>()
                .HasOne(s => s.OtherItem)
                .WithMany()
                .HasForeignKey(s => s.OtherItemId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}