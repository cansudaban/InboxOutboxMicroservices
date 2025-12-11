using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using Polly;
using Polly.Retry;

namespace JobService.Services
{
    public class MessageBusService : IDisposable
    {
        private readonly IConnection? _connection;
        private readonly IModel? _channel;
        private readonly ILogger<MessageBusService> _logger;
        private readonly AsyncRetryPolicy _retryPolicy;
        private const int MaxRetryAttempts = 3;

        public MessageBusService(ILogger<MessageBusService> logger)
        {
            _logger = logger;
            
            // Yeniden deneme politikası tanımla
            _retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    MaxRetryAttempts, 
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning($"RabbitMQ publish retry {retryCount}/{MaxRetryAttempts} after {timeSpan.TotalSeconds}s due to: {exception.Message}");
                    });
            
            try
            {
                var factory = new ConnectionFactory() { HostName = "localhost" };
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();
                
                // Kalıcı exchange tanımla
                _channel.ExchangeDeclare(
                    exchange: "job_events", 
                    type: "fanout",
                    durable: true,
                    autoDelete: false);
                
                _logger.LogInformation("Connected to RabbitMQ");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to RabbitMQ");
                // Connection hatalarını yönetmek için burada bir şey yapmıyoruz
                // PublishMessage metodu içinde null kontrolleri ve hata yakalama var
            }
        }

        public async Task<bool> PublishMessage<T>(T message, string eventName)
        {
            if (_channel == null)
            {
                _logger.LogError("RabbitMQ bağlantısı kurulamadı. Mesaj gönderilemedi.");
                return false;
            }

            try
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    var jsonMessage = JsonSerializer.Serialize(message);
                    var body = Encoding.UTF8.GetBytes(jsonMessage);

                    var properties = _channel.CreateBasicProperties();
                    properties.MessageId = Guid.NewGuid().ToString();
                    properties.Persistent = true; // Mesaj kalıcılığını sağla
                    properties.Headers = new Dictionary<string, object>
                    {
                        { "event_name", eventName },
                        { "timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }
                    };

                    _channel.BasicPublish(
                        exchange: "job_events",
                        routingKey: string.Empty,
                        basicProperties: properties,
                        body: body);

                    _logger.LogInformation($"Published message: {eventName} with ID: {properties.MessageId}");
                    return true;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to publish message {eventName} after {MaxRetryAttempts} attempts");
                return false;
            }
        }

        public void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
}
