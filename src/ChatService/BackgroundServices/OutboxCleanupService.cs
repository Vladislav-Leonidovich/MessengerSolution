using ChatService.Data;
using Microsoft.EntityFrameworkCore;

namespace ChatService.BackgroundServices
{
    public class OutboxCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OutboxCleanupService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromDays(1); // Запускати раз на добу
        private readonly TimeSpan _retentionPeriod = TimeSpan.FromDays(7); // Зберігати повідомлення 7 днів

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

                // Чекаємо до наступного запуску
                await Task.Delay(_interval, stoppingToken);
            }

            _logger.LogInformation("Outbox Cleanup Service зупинено");
        }

        private async Task CleanupOutboxMessagesAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ChatDbContext>();

            // Знаходимо дату, перед якою повідомлення можна видаляти
            var cutoffDate = DateTime.UtcNow.Subtract(_retentionPeriod);

            // Видаляємо старі оброблені повідомлення
            var oldMessages = await dbContext.OutboxMessages
                .Where(m => m.ProcessedAt != null && m.ProcessedAt < cutoffDate)
                .ToListAsync(stoppingToken);

            if (oldMessages.Any())
            {
                dbContext.OutboxMessages.RemoveRange(oldMessages);
                var count = await dbContext.SaveChangesAsync(stoppingToken);

                _logger.LogInformation("Видалено {Count} старих Outbox повідомлень", count);
            }
        }
    }
}
