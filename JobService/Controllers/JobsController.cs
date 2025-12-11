using Microsoft.AspNetCore.Mvc;
using JobService.Models;
using JobService.Services;

namespace JobService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    private readonly JobService.Services.JobService _jobService;
    private readonly ILogger<JobsController> _logger;

    public JobsController(JobService.Services.JobService jobService, ILogger<JobsController> logger)
    {
        _jobService = jobService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllJobs()
    {
        var jobs = await _jobService.GetJobsAsync();
        return Ok(jobs);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetJob(Guid id)
    {
        var job = await _jobService.GetJobAsync(id);
        if (job == null)
        {
            return NotFound();
        }
        return Ok(job);
    }

    [HttpPost]
    public async Task<IActionResult> PostJob([FromBody] PostJobRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var job = new Job
        {
            CompanyName = request.CompanyName,
            JobTitle = request.JobTitle,
            JobDescription = request.JobDescription,
            Location = request.Location,
            EmploymentType = request.EmploymentType,
            SalaryMin = request.SalaryMin,
            SalaryMax = request.SalaryMax,
            RequiredSkills = request.RequiredSkills,
            ApplicationDeadline = request.ApplicationDeadline
        };

        try 
        {
            var createdJob = await _jobService.PostJobAsync(job);
            return CreatedAtAction(nameof(GetJob), new { id = createdJob.Id }, createdJob);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error posting job");
            return StatusCode(500, "An error occurred while posting the job");
        }
    }
}

public class PostJobRequest
{
    public string CompanyName { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string JobDescription { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string EmploymentType { get; set; } = string.Empty;
    public decimal SalaryMin { get; set; }
    public decimal SalaryMax { get; set; }
    public List<string> RequiredSkills { get; set; } = new();
    public DateTime ApplicationDeadline { get; set; }
}
