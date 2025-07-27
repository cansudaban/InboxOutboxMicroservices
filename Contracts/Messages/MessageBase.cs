using System.Text.Json.Serialization;

namespace Contracts.Messages
{
    public class MessageBase
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string MessageType { get; set; } = string.Empty;
    }
}
