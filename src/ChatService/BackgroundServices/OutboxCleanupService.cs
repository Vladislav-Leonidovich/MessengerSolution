using ChatService.Data;
using ChatService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatService.BackgroundServices
{
    public class OutboxCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OutboxCleanupService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromHours(6); // Запускати кожні 6 годин
        private readonly TimeSpan _retentionPeriod = TimeSpan.FromDays(7); // Зберігати 7 днів

        public OutboxCleanupService(
            IServiceScopeFactory scopeFactory,
            ILogger<OutboxCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Outbox Cleanup Service запущений");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupOutboxMessagesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Помилка під час очистки Outbox повідомлень");
                }

                await Task.Delay(_interval, stoppingToken);
            }

            _logger.LogInformation("Outbox Cleanup Service зупинено");
        }

        private async Task CleanupOutboxMessagesAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ChatDbContext>();

            var cutoffDate = DateTime.UtcNow.Subtract(_retentionPeriod);

            // Видаляємо оброблені повідомлення старіші за період зберігання
            var oldMessages = await dbContext.OutboxMessages
                .Where(m => (m.Status == OutboxMessageStatus.Processed || m.Status == OutboxMessageStatus.Cancelled) &&
                       m.ProcessedAt < cutoffDate)
                .ToListAsync(stoppingToken);

            if (oldMessages.Any())
            {
                dbContext.OutboxMessages.RemoveRange(oldMessages);
                var deletedCount = await dbContext.SaveChangesAsync(stoppingToken);

                _logger.LogInformation("Видалено {Count} старих Outbox повідомлень", deletedCount);
            }

            // Відстежуємо "застряглі" повідомлення (в обробці довше 10 хвилин)
            var stuckProcessingTime = TimeSpan.FromMinutes(10);
            var stuckMessages = await dbContext.OutboxMessages
                .Where(m => m.Status == OutboxMessageStatus.Processing &&
                       m.ProcessedAt < DateTime.UtcNow.Subtract(stuckProcessingTime))
                .ToListAsync(stoppingToken);

            if (stuckMessages.Any())
            {
                foreach (var message in stuckMessages)
                {
                    message.Status = OutboxMessageStatus.Pending;
                    message.ProcessedAt = null;
                    message.Error = "Відновлено після зависання: " + message.Error;
                }

                await dbContext.SaveChangesAsync(stoppingToken);
                _logger.LogWarning("Відновлено {Count} 'застряглих' Outbox повідомлень", stuckMessages.Count);
            }
        }
    }
}
