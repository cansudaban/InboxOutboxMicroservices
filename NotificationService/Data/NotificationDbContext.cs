using Microsoft.EntityFrameworkCore;
using NotificationService.Models;

namespace NotificationService.Data
{
    public class NotificationDbContext : DbContext
    {
        public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options)
        {
        }

        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<InboxMessage> InboxMessages { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.RecipientEmail).IsRequired().HasMaxLength(200);
                entity.Property(e => e.RecipientName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.NotificationType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Subject).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Body).IsRequired();
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
