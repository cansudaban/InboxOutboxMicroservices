using Microsoft.EntityFrameworkCore;
using ApplicationService.Models;

namespace ApplicationService.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<JobApplication> Applications { get; set; } = null!;
        public DbSet<InboxMessage> InboxMessages { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<JobApplication>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ApplicantName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.ApplicantEmail).IsRequired().HasMaxLength(200);
                entity.Property(e => e.ApplicantPhone).HasMaxLength(50);
                entity.Property(e => e.ApplicationStatus).IsRequired().HasMaxLength(50);
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
