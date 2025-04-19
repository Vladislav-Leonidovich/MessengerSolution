using System.Text.Json;
using MassTransit;
using MessageService.Models;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts;

namespace MessageService.BackgroundServices
{
    public class OutboxProcessorService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OutboxProcessorService> _logger;
        private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);

        public OutboxProcessorService(
            IServiceScopeFactory scopeFactory,
            ILogger<OutboxProcessorService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Outbox Processor Service запущений");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessOutboxMessagesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Помилка під час обробки Outbox повідомлень");
                }

                await Task.Delay(_pollingInterval, stoppingToken);
            }

            _logger.LogInformation("Outbox Processor Service зупинено");
        }

        private async Task ProcessOutboxMessagesAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DbContext>();
            var bus = scope.ServiceProvider.GetRequiredService<IBus>();

            var messages = await dbContext.Set<OutboxMessage>()
                .Where(m => m.ProcessedAt == null && m.RetryCount < 5)
                .OrderBy(m => m.CreatedAt)
                .Take(50)
                .ToListAsync(stoppingToken);

            foreach (var message in messages)
            {
                try
                {
                    await PublishMessageToBusAsync(bus, message, stoppingToken);

                    message.ProcessedAt = DateTime.UtcNow;
                    await dbContext.SaveChangesAsync(stoppingToken);

                    _logger.LogInformation("Outbox повідомлення {MessageId} успішно оброблено", message.Id);
                }
                catch (Exception ex)
                {
                    message.RetryCount++;
                    message.Error = ex.Message;
                    await dbContext.SaveChangesAsync(stoppingToken);

                    _logger.LogWarning(ex, "Помилка під час обробки Outbox повідомлення {MessageId}. Спроба {RetryCount}/5",
                        message.Id, message.RetryCount);
                }
            }
        }

        private async Task PublishMessageToBusAsync(IBus bus, OutboxMessage message, CancellationToken cancellationToken)
        {
            // Логика маппинга типов и публикации
            switch (message.EventType)
            {
                case nameof(MessageCreatedEvent):
                    var messageCreatedEvent = JsonSerializer.Deserialize<MessageCreatedEvent>(message.EventData);
                    if (messageCreatedEvent != null)
                    {
                        await bus.Publish(messageCreatedEvent, cancellationToken);
                    }
                    break;

                case nameof(ChatDeletedEvent):
                    var chatDeletedEvent = JsonSerializer.Deserialize<ChatDeletedEvent>(message.EventData);
                    if (chatDeletedEvent != null)
                    {
                        await bus.Publish(chatDeletedEvent, cancellationToken);
                    }
                    break;

                // Другие типы событий...

                default:
                    _logger.LogWarning("Невідомий тип події: {EventType}", message.EventType);
                    break;
            }
        }
    }
}
