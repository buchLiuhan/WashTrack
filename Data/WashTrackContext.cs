using Microsoft.EntityFrameworkCore;
using WashTrack.Models;
namespace WashTrack.Data
{
    public class WashTrackContext : DbContext
    {
        public WashTrackContext(DbContextOptions<WashTrackContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<TransactionItem> TransactionItems { get; set; }
        public DbSet<Inventory> Inventories { get; set; }
        public DbSet<InventoryUsageHistory> InventoryUsageHistories { get; set; }

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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Customer)
                .WithMany()
                .HasForeignKey(t => t.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TransactionItem>()
                .HasOne(ti => ti.Transaction)
                .WithMany(t => t.Items)
                .HasForeignKey(ti => ti.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TransactionItem>()
                .HasOne(ti => ti.Service)
                .WithMany()
                .HasForeignKey(ti => ti.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

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

            // ===== SUPPLY LINKS =====
            // SetNull: if an inventory item is ever permanently deleted,
            // the service simply loses the link instead of breaking.
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