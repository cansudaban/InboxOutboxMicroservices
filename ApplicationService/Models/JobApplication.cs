namespace ApplicationService.Models
{
    public class JobApplication
    {
        public Guid Id { get; set; }
        public Guid JobId { get; set; }
        public string ApplicantName { get; set; } = string.Empty;
        public string ApplicantEmail { get; set; } = string.Empty;
        public string ApplicantPhone { get; set; } = string.Empty;
        public string Resume { get; set; } = string.Empty; // URL or file path
        public string CoverLetter { get; set; } = string.Empty;
        public int YearsOfExperience { get; set; }
        public List<string> Skills { get; set; } = new();
        public string ApplicationStatus { get; set; } = "Pending"; // Pending, Reviewing, Interviewed, Accepted, Rejected
        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}

