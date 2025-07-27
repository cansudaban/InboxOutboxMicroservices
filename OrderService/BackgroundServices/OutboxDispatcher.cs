using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Services;
using System.Text.Json;
using Contracts.Events;
using OrderService.Models;
using Polly;
using Polly.Retry;

namespace OrderService.BackgroundServices
{
    public class OutboxDispatcher : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OutboxDispatcher> _logger;
        private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(10);
        private readonly AsyncRetryPolicy _retryPolicy;
        private const int MaxRetryAttempts = 3;

        public OutboxDispatcher(IServiceScopeFactory scopeFactory, ILogger<OutboxDispatcher> logger)
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
                        _logger.LogWarning($"Outbox processing retry {retryCount}/{MaxRetryAttempts} after {timeSpan.TotalSeconds}s due to: {exception.Message}");
                    });
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("OutboxDispatcher service starting");

            while (!stoppingToken.IsCancellationRequested)
            {
                await ProcessOutboxMessages(stoppingToken);
                await Task.Delay(_pollingInterval, stoppingToken);
            }
        }

        private async Task ProcessOutboxMessages(CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
                var messageBusService = scope.ServiceProvider.GetRequiredService<MessageBusService>();

                var messages = await dbContext.OutboxMessages
                    .Where(m => m.ProcessedAt == null && m.Error == null)
                    .OrderBy(m => m.CreatedAt)
                    .Take(50)
                    .ToListAsync(stoppingToken);

                if (messages.Count == 0)
                {
                    return;
                }

                _logger.LogInformation("Found {Count} messages to process", messages.Count);

                foreach (var message in messages)
                {
                    try
                    {
                        bool publishResult = false;
                        
                        // Farklı event türleri için işleme
                        switch (message.EventType)
                        {
                            case "OrderCreated":
                                var orderEvent = JsonSerializer.Deserialize<OrderCreatedEventDto>(message.Content);
                                if (orderEvent != null)
                                {
                                    publishResult = await messageBusService.PublishMessage(orderEvent, message.EventType);
                                }
                                break;
                            default:
                                _logger.LogWarning("Unknown event type: {EventType}", message.EventType);
                                break;
                        }

                        if (publishResult)
                        {
                            // Başarılı gönderimde işlendi olarak işaretle
                            message.ProcessedAt = DateTime.UtcNow;
                        }
                        else 
                        {
                            // Gönderim başarısız oldu, hata durumunu kaydet
                            message.Error = "Failed to publish message to message bus";
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing outbox message {MessageId}", message.Id);
                        message.Error = ex.Message;
                        
                        // Belirli bir sayıdan fazla deneme yapıldıysa özel işlem yap
                        // Bu örnekte hata durumunu kaydetmekle yetiniyoruz
                    }
                }

                await dbContext.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OutboxDispatcher");
            }
        }
        
        // Hatalı mesajları işleme metodu (opsiyonel olarak düzenli çalıştırılabilir)
        public async Task ProcessFailedMessages(CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
                
                // Belirli bir süredir hatalı olan mesajları al
                var cutoffTime = DateTime.UtcNow.AddHours(-1);
                var failedMessages = await dbContext.OutboxMessages
                    .Where(m => m.ProcessedAt == null && m.Error != null && m.CreatedAt < cutoffTime)
                    .OrderBy(m => m.CreatedAt)
                    .Take(20)
                    .ToListAsync(stoppingToken);
                
                if (failedMessages.Count == 0)
                {
                    return;
                }
                
                _logger.LogInformation("Found {Count} failed messages to reprocess", failedMessages.Count);
                
                // Burada başarısız mesajları işlemek için özel bir mantık eklenebilir
                // Örneğin, ayrı bir ölü mektup kuyruğuna gönderilebilir veya admin bilgilendirilebilir
                
                // Mesajları işlendi olarak işaretle veya ayrı bir tablo/duruma aktar
                foreach (var message in failedMessages)
                {
                    // Bu örnekte sadece bir hata notu ekliyoruz
                    message.Error += " | Permanently failed, manual intervention required.";
                }
                
                await dbContext.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing failed messages");
            }
        }
    }
}
