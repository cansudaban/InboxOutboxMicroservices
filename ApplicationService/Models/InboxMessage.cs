using System.Text.Json;

namespace ApplicationService.Models
{
    public class InboxMessage
    {
        public Guid Id { get; set; }
        public string MessageId { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }
        public string? Error { get; set; }
    }
}
