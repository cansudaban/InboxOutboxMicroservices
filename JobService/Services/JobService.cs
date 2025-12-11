using Contracts.Events;
using Microsoft.EntityFrameworkCore;
using JobService.Data;
using JobService.Models;
using System.Text.Json;

namespace JobService.Services
{
    public class JobService
    {
        private readonly JobDbContext _dbContext;
        private readonly ILogger<JobService> _logger;

        public JobService(JobDbContext dbContext, ILogger<JobService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<Job> PostJobAsync(Job job)
        {
            // Begin transaction to ensure both job and outbox message are saved atomically
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                // Set job properties
                job.Id = Guid.NewGuid();
                job.PostedAt = DateTime.UtcNow;
                job.IsActive = true;

                // Save job to database
                _dbContext.Jobs.Add(job);

                // Create JobPosted event and save to outbox
                var jobPostedEvent = new JobPostedEventDto
                {
                    JobId = job.Id,
                    CompanyName = job.CompanyName,
                    JobTitle = job.JobTitle,
                    JobDescription = job.JobDescription,
                    Location = job.Location,
                    EmploymentType = job.EmploymentType,
                    SalaryMin = job.SalaryMin,
                    SalaryMax = job.SalaryMax,
                    RequiredSkills = job.RequiredSkills,
                    PostedAt = job.PostedAt,
                    ApplicationDeadline = job.ApplicationDeadline
                };

                // Create outbox message
                var outboxMessage = OutboxMessage.Create("JobPosted", jobPostedEvent);
                _dbContext.OutboxMessages.Add(outboxMessage);

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Job posted: {JobId}", job.Id);
                return job;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error posting job");
                throw;
            }
        }

        public async Task<IEnumerable<Job>> GetJobsAsync()
        {
            return await _dbContext.Jobs
                .Where(j => j.IsActive)
                .ToListAsync();
        }

        public async Task<Job?> GetJobAsync(Guid id)
        {
            return await _dbContext.Jobs
                .FirstOrDefaultAsync(j => j.Id == id);
        }
    }
}
