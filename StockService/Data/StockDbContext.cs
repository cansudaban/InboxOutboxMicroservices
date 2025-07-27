using Microsoft.EntityFrameworkCore;
using StockService.Models;

namespace StockService.Data
{
    public class StockDbContext : DbContext
    {
        public StockDbContext(DbContextOptions<StockDbContext> options) : base(options)
        {
        }

        public DbSet<StockItem> StockItems { get; set; } = null!;
        public DbSet<InboxMessage> InboxMessages { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<StockItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ProductName).IsRequired();
            });

            modelBuilder.Entity<InboxMessage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.MessageId).IsRequired();
                entity.Property(e => e.EventType).IsRequired();
                entity.Property(e => e.Content).IsRequired();
                
                // Unique index to ensure idempotency
                entity.HasIndex(e => e.MessageId).IsUnique();
            });
        }
    }
}
