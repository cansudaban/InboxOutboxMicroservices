using System.Text;
using System.Text.Json;
using Contracts.Events;
using Polly;
using Polly.Retry;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using NotificationService.Data;
using NotificationService.Models;

namespace NotificationService.Services
{
    public class MessageConsumerService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MessageConsumerService> _logger;
        private readonly IConnection? _connection;
        private readonly IModel? _channel;
        private readonly string _queueName;
        private readonly string _deadLetterQueueName;
        private readonly AsyncRetryPolicy _retryPolicy;
        private const int MaxRetryAttempts = 3;

        public MessageConsumerService(IServiceScopeFactory scopeFactory, ILogger<MessageConsumerService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            
            // Yeniden deneme politikası tanımla
            _retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    MaxRetryAttempts, 
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning($"Retry {retryCount}/{MaxRetryAttempts} after {timeSpan.TotalSeconds}s due to: {exception.Message}");
                    });
            
            try 
            {
                var factory = new ConnectionFactory { HostName = "localhost" };
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();
                
                // Ölü mektup kuyruğu için exchange tanımla
                _channel.ExchangeDeclare(exchange: "dead_letter_exchange", type: "fanout", durable: true);
                
                // Ölü mektup kuyruğu oluştur
                _deadLetterQueueName = "dead_letter_queue_notification";
                _channel.QueueDeclare(
                    queue: _deadLetterQueueName, 
                    durable: true, 
                    exclusive: false, 
                    autoDelete: false, 
                    arguments: null);
                
                // Ölü mektup kuyruğunu exchange'e bağla
                _channel.QueueBind(
                    queue: _deadLetterQueueName,
                    exchange: "dead_letter_exchange",
                    routingKey: string.Empty);
                
                // Ana event exchange tanımla
                _channel.ExchangeDeclare(exchange: "job_events", type: "fanout", durable: true);
                
                // Ölü mektup yapılandırması ile ana kuyruk tanımla
                var arguments = new Dictionary<string, object>
                {
                    { "x-dead-letter-exchange", "dead_letter_exchange" }
                };
                
                // Servis özel kuyruğu oluştur
                _queueName = "notification_service_queue";
                _channel.QueueDeclare(
                    queue: _queueName, 
                    durable: true, 
                    exclusive: false, 
                    autoDelete: false, 
                    arguments: arguments);
                
                // Ana kuyruğu exchange'e bağla
                _channel.QueueBind(
                    queue: _queueName,
                    exchange: "job_events",
                    routingKey: string.Empty);
                
                // Adil dağıtım için
                _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "RabbitMQ bağlantısı kurulamadı");
            }
        }
        
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Notification MessageConsumerService started listening");
            
            if (_channel == null)
            {
                _logger.LogError("RabbitMQ kanalı oluşturulamadı. Servis çalışmayı durduruyor.");
                return Task.CompletedTask;
            }
            
            var consumer = new EventingBasicConsumer(_channel);
            
            consumer.Received += async (model, ea) =>
            {
                var messageId = ea.BasicProperties.MessageId;
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                
                _logger.LogInformation($"Received message: {messageId}");
                
                try
                {
                    // Process the message within a scope and retry policy
                    await _retryPolicy.ExecuteAsync(async () => 
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

                        // Check if we've already processed this message (idempotency check)
                        var existingMessage = await dbContext.InboxMessages
                            .FirstOrDefaultAsync(m => m.MessageId == messageId);

                        if (existingMessage != null)
                        {
                            _logger.LogInformation($"Message {messageId} already processed. Skipping.");
                            return; // Skip processing, but don't ack yet
                        }
                        
                        // Save message to inbox
                        var inboxMessage = new InboxMessage
                        {
                            MessageId = messageId,
                            EventType = "JobPosted",
                            Content = message,
                            CreatedAt = DateTime.UtcNow
                        };
                        
                        dbContext.InboxMessages.Add(inboxMessage);
                        
                        // Process the message based on event type
                        var jobPostedEvent = JsonSerializer.Deserialize<JobPostedEventDto>(message);
                        if (jobPostedEvent != null)
                        {
                            await SendJobNotifications(jobPostedEvent, dbContext);
                            inboxMessage.ProcessedAt = DateTime.UtcNow;
                        }
                        
                        await dbContext.SaveChangesAsync();
                    });
                    
                    // Başarılı olursa onaylama işlemi yap
                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to process message {messageId} after {MaxRetryAttempts} attempts. Moving to dead-letter queue.");
                    
                    // Mesajı reddet ve ölü mektup kuyruğuna taşı
                    _channel.BasicReject(ea.DeliveryTag, false);
                    
                    // Hata bilgisini kaydet (isteğe bağlı)
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
                        
                        // Mesajın işleme durumunu güncelle
                        var inboxMessage = await dbContext.InboxMessages
                            .FirstOrDefaultAsync(m => m.MessageId == messageId);
                            
                        if (inboxMessage != null)
                        {
                            inboxMessage.Error = ex.Message;
                            await dbContext.SaveChangesAsync();
                        }
                    }
                    catch (Exception logEx)
                    {
                        _logger.LogError(logEx, "Failed to log message error state");
                    }
                }
            };
            
            _channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);
            
            return Task.CompletedTask;
        }
        
        private async Task SendJobNotifications(JobPostedEventDto jobEvent, NotificationDbContext dbContext)
        {
            // Create notification records
            // In a real system, you would:
            // 1. Query subscribed users based on job criteria (location, skills, etc.)
            // 2. Send actual emails/SMS
            // 3. Track delivery status
            
            // For this demo, we'll create sample notification records
            var notification = new Notification
            {
                JobId = jobEvent.JobId,
                RecipientEmail = "subscribers@example.com", // In reality, would be actual subscriber emails
                RecipientName = "Job Seekers",
                NotificationType = "JobPosted",
                Subject = $"New Job Opportunity: {jobEvent.JobTitle} at {jobEvent.CompanyName}",
                Body = $"A new job has been posted:\n\n" +
                       $"Position: {jobEvent.JobTitle}\n" +
                       $"Company: {jobEvent.CompanyName}\n" +
                       $"Location: {jobEvent.Location}\n" +
                       $"Employment Type: {jobEvent.EmploymentType}\n" +
                       $"Salary Range: {jobEvent.SalaryMin:C} - {jobEvent.SalaryMax:C}\n" +
                       $"Required Skills: {string.Join(", ", jobEvent.RequiredSkills)}\n" +
                       $"Application Deadline: {jobEvent.ApplicationDeadline:d}\n\n" +
                       $"Apply now!",
                CreatedAt = DateTime.UtcNow,
                IsSent = true, // Simulating successful send
                SentAt = DateTime.UtcNow
            };
            
            dbContext.Notifications.Add(notification);
            _logger.LogInformation($"Notification created for job {jobEvent.JobId}: {jobEvent.JobTitle}");
            
            await Task.CompletedTask;
        }
        
        // Servis için ölü mektup kuyruğu işleme metodu ekle (opsiyonel)
        public Task ProcessDeadLetterQueueAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting to process dead letter queue");
            
            if (_channel == null)
            {
                _logger.LogError("RabbitMQ kanalı oluşturulamadı. Ölü mektup kuyruğu işlenemiyor.");
                return Task.CompletedTask;
            }
            
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (model, ea) =>
            {
                var messageId = ea.BasicProperties.MessageId;
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                
                _logger.LogWarning($"Processing dead letter message: {messageId}");
                
                // Burada ölü mektupları işleme mantığı eklenebilir
                // Örneğin, özel bir veritabanı tablosuna kaydedilebilir,
                // bir admin paneline bildirim gönderilebilir vb.
                
                _channel.BasicAck(ea.DeliveryTag, false);
            };
            
            _channel.BasicConsume(queue: _deadLetterQueueName, autoAck: false, consumer: consumer);
            
            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
            base.Dispose();
        }
    }
}
