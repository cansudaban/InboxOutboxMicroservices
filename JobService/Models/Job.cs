namespace JobService.Models
{
    public class Job
    {
        public Guid Id { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public string JobDescription { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string EmploymentType { get; set; } = string.Empty; // Full-time, Part-time, Contract
        public decimal SalaryMin { get; set; }
        public decimal SalaryMax { get; set; }
        public List<string> RequiredSkills { get; set; } = new();
        public DateTime PostedAt { get; set; }
        public DateTime ApplicationDeadline { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
