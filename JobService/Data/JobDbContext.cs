using Microsoft.EntityFrameworkCore;
using JobService.Models;

namespace JobService.Data
{
    public class JobDbContext : DbContext
    {
        public JobDbContext(DbContextOptions<JobDbContext> options) : base(options)
        {
        }

        public DbSet<Job> Jobs { get; set; } = null!;
        public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Job>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CompanyName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.JobTitle).IsRequired().HasMaxLength(200);
                entity.Property(e => e.JobDescription).IsRequired();
                entity.Property(e => e.Location).IsRequired().HasMaxLength(100);
                entity.Property(e => e.EmploymentType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.SalaryMin).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.SalaryMax).HasColumnType("decimal(18, 2)");
            });

            modelBuilder.Entity<OutboxMessage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.EventType).IsRequired();
                entity.Property(e => e.Content).IsRequired();
            });
        }
    }
}
