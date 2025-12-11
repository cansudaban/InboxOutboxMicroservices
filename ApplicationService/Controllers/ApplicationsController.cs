using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ApplicationService.Data;
using ApplicationService.Models;

namespace ApplicationService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ApplicationsController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<ApplicationsController> _logger;

        public ApplicationsController(ApplicationDbContext dbContext, ILogger<ApplicationsController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllApplications()
        {
            var applications = await _dbContext.Applications.ToListAsync();
            return Ok(applications);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetApplicationById(Guid id)
        {
            var application = await _dbContext.Applications
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null)
            {
                return NotFound();
            }

            return Ok(application);
        }

        [HttpGet("job/{jobId:guid}")]
        public async Task<IActionResult> GetApplicationsByJobId(Guid jobId)
        {
            var applications = await _dbContext.Applications
                .Where(a => a.JobId == jobId)
                .ToListAsync();

            return Ok(applications);
        }

        // Bu endpoint elle başvuru oluşturmak için test amaçlı eklenmiştir
        [HttpPost]
        public async Task<IActionResult> CreateApplication([FromBody] CreateApplicationRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var application = new JobApplication
            {
                Id = Guid.NewGuid(),
                JobId = request.JobId,
                ApplicantName = request.ApplicantName,
                ApplicantEmail = request.ApplicantEmail,
                ApplicantPhone = request.ApplicantPhone,
                Resume = request.Resume,
                CoverLetter = request.CoverLetter,
                YearsOfExperience = request.YearsOfExperience,
                Skills = request.Skills,
                ApplicationStatus = "Pending",
                AppliedAt = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow
            };

            try
            {
                _dbContext.Applications.Add(application);
                await _dbContext.SaveChangesAsync();
                return CreatedAtAction(nameof(GetApplicationById), new { id = application.Id }, application);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating application");
                return StatusCode(500, "An error occurred while creating the application");
            }
        }
    }

    public class CreateApplicationRequest
    {
        public Guid JobId { get; set; }
        public string ApplicantName { get; set; } = string.Empty;
        public string ApplicantEmail { get; set; } = string.Empty;
        public string ApplicantPhone { get; set; } = string.Empty;
        public string Resume { get; set; } = string.Empty;
        public string CoverLetter { get; set; } = string.Empty;
        public int YearsOfExperience { get; set; }
        public List<string> Skills { get; set; } = new();
    }
}
