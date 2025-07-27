using System.Text.Json;

namespace OrderService.Models
{
    public class OutboxMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string EventType { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }
        public string? Error { get; set; }
        
        public static OutboxMessage Create<T>(string eventType, T data)
        {
            return new OutboxMessage
            {
                EventType = eventType,
                Content = JsonSerializer.Serialize(data)
            };
        }
    }
}
