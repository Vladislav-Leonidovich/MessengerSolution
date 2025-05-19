using System.Text.Json;
using ChatService.Data;
using ChatService.Models;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Sagas;

namespace ChatService.BackgroundServices
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

            using var timer = new PeriodicTimer(_pollingInterval);

            while (!stoppingToken.IsCancellationRequested &&
                await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await ProcessOutboxMessagesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Помилка під час обробки Outbox повідомлень");
                }
            }

            _logger.LogInformation("Outbox Processor Service зупинено");
        }

        private async Task ProcessOutboxMessagesAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
                var bus = scope.ServiceProvider.GetRequiredService<IBus>();

                var messages = await dbContext.OutboxMessages
                    .Where(m => m.ProcessedAt == null && m.RetryCount < 5)
                    .OrderBy(m => m.CreatedAt)
                    .Take(50)
                    .ToListAsync(stoppingToken);

                if (messages.Any())
                {
                    _logger.LogInformation("Знайдено {Count} непроцесованих повідомлень в Outbox", messages.Count);
                }

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Критична помилка під час обробки Outbox повідомлень");
            }
        }

        private async Task PublishMessageToBusAsync(IBus bus, OutboxMessage message, CancellationToken cancellationToken)
        {
            // Це перелік всіх типів подій, які ми обробляємо
            switch (message.EventType)
            {
                case nameof(ChatCreationStartedEvent):
                    var chatCreationStartedEvent = JsonSerializer.Deserialize<ChatCreationStartedEvent>(message.EventData);
                    if (chatCreationStartedEvent != null)
                    {
                        await bus.Publish(chatCreationStartedEvent, cancellationToken);
                    }
                    break;

                case nameof(ChatRoomCreatedEvent):
                    var chatRoomCreatedEvent = JsonSerializer.Deserialize<ChatRoomCreatedEvent>(message.EventData);
                    if (chatRoomCreatedEvent != null)
                    {
                        await bus.Publish(chatRoomCreatedEvent, cancellationToken);
                    }
                    break;

                case nameof(MessageServiceNotifiedEvent):
                    var messageServiceNotifiedEvent = JsonSerializer.Deserialize<MessageServiceNotifiedEvent>(message.EventData);
                    if (messageServiceNotifiedEvent != null)
                    {
                        await bus.Publish(messageServiceNotifiedEvent, cancellationToken);
                    }
                    break;

                case nameof(ChatCreationFailedEvent):
                    var chatCreationFailedEvent = JsonSerializer.Deserialize<ChatCreationFailedEvent>(message.EventData);
                    if (chatCreationFailedEvent != null)
                    {
                        await bus.Publish(chatCreationFailedEvent, cancellationToken);
                    }
                    break;

                case nameof(ChatCreationCompensatedEvent):
                    var chatCreationCompensatedEvent = JsonSerializer.Deserialize<ChatCreationCompensatedEvent>(message.EventData);
                    if (chatCreationCompensatedEvent != null)
                    {
                        await bus.Publish(chatCreationCompensatedEvent, cancellationToken);
                    }
                    break;

                default:
                    _logger.LogWarning("Невідомий тип події: {EventType}", message.EventType);
                    break;
            }
        }
    }
}
