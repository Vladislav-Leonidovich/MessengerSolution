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

        // Налаштування політики повторних спроб
        private readonly int _maxRetryCount = 5;
        private readonly TimeSpan[] _retryDelays = new[]
        {
            TimeSpan.FromSeconds(10),    // 1-ша повторна спроба через 10 секунд
            TimeSpan.FromMinutes(1),     // 2-га через 1 хвилину
            TimeSpan.FromMinutes(5),     // 3-тя через 5 хвилин
            TimeSpan.FromMinutes(15),    // 4-та через 15 хвилин
            TimeSpan.FromMinutes(60)     // 5-та через 1 годину
        };

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

                // Отримуємо повідомлення для обробки
                var messages = await dbContext.OutboxMessages
                    .Where(m => m.Status == OutboxMessageStatus.Pending &&
                           (m.NextRetryAt == null || m.NextRetryAt <= DateTime.UtcNow) &&
                           m.RetryCount < _maxRetryCount)
                    .OrderBy(m => m.CreatedAt)
                    .Take(20) // Обробляємо пакетами по 20 повідомлень
                    .ToListAsync(stoppingToken);

                if (messages.Any())
                {
                    _logger.LogInformation("Знайдено {Count} повідомлень Outbox для обробки", messages.Count);
                }

                foreach (var message in messages)
                {
                    try
                    {
                        // Позначаємо повідомлення як таке, що обробляється
                        message.Status = OutboxMessageStatus.Processing;
                        await dbContext.SaveChangesAsync(stoppingToken);

                        // Публікуємо повідомлення
                        await PublishMessageToBusAsync(bus, message, stoppingToken);

                        // Позначаємо повідомлення як успішно оброблене
                        message.Status = OutboxMessageStatus.Processed;
                        message.ProcessedAt = DateTime.UtcNow;
                        message.Error = null;

                        await dbContext.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation("Повідомлення Outbox {MessageId} успішно оброблено", message.Id);
                    }
                    catch (Exception ex)
                    {
                        message.RetryCount++;
                        message.Error = ex.Message;

                        // Якщо досягнуто максимальної кількості спроб
                        if (message.RetryCount >= _maxRetryCount)
                        {
                            message.Status = OutboxMessageStatus.Failed;
                            _logger.LogError(ex, "Повідомлення Outbox {MessageId} не вдалося обробити після {RetryCount} спроб",
                                message.Id, message.RetryCount);
                        }
                        else
                        {
                            // Розраховуємо час наступної спроби з експоненційною затримкою
                            int delayIndex = Math.Min(message.RetryCount - 1, _retryDelays.Length - 1);
                            message.NextRetryAt = DateTime.UtcNow.Add(_retryDelays[delayIndex]);
                            message.Status = OutboxMessageStatus.Pending;

                            _logger.LogWarning(ex, "Помилка при обробці повідомлення Outbox {MessageId}. Наступна спроба {RetryCount}/{MaxRetry} через {Delay}",
                                message.Id, message.RetryCount, _maxRetryCount, _retryDelays[delayIndex]);
                        }

                        await dbContext.SaveChangesAsync(stoppingToken);
                    }
                }

                // Також перевіряємо наявність провалених повідомлень для звітності
                var failedCount = await dbContext.OutboxMessages
                    .CountAsync(m => m.Status == OutboxMessageStatus.Failed, stoppingToken);

                if (failedCount > 0)
                {
                    _logger.LogWarning("Знайдено {Count} повідомлень Outbox, які не вдалося обробити", failedCount);
                    // Можна додати відправку повідомлення адміністраторам або в моніторинг
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Критична помилка при обробці повідомлень Outbox");
            }
        }

        private async Task PublishMessageToBusAsync(IBus bus, OutboxMessage message, CancellationToken cancellationToken)
        {
            // Логіка публікації подій залишається без змін
            switch (message.EventType)
            {
                case nameof(ChatCreationStartedEvent):
                    var chatCreationStartedEvent = JsonSerializer.Deserialize<ChatCreationStartedEvent>(message.EventData);
                    if (chatCreationStartedEvent != null)
                    {
                        await bus.Publish(chatCreationStartedEvent, cancellationToken);
                    }
                    break;

                // Інші обробники подій...

                default:
                    _logger.LogWarning("Невідомий тип події: {EventType}", message.EventType);
                    break;
            }
        }
    }
}
