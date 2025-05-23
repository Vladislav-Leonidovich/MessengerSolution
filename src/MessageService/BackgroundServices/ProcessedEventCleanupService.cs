using MessageService.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts;

namespace MessageService.BackgroundServices
{
    // Сервіс для очищення старих записів
    public class ProcessedEventCleanupService : BackgroundService
    {
        // Видаляти записи старші за 30 днів
        private readonly TimeSpan _retentionPeriod = TimeSpan.FromDays(30);
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ProcessedEventCleanupService> _logger;

        public ProcessedEventCleanupService(IServiceScopeFactory scopeFactory, ILogger<ProcessedEventCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Processed Event Cleanup Service запущений");

            while (!stoppingToken.IsCancellationRequested)
            {
                var cutoffDate = DateTime.UtcNow.Subtract(_retentionPeriod);
                using var scope = _scopeFactory.CreateScope();
                var _dbContext = scope.ServiceProvider.GetRequiredService<MessageDbContext>();

                // Видалення старих записів
                var oldRecords = await _dbContext.Set<ProcessedEvent>()
                    .Where(pe => pe.ProcessedAt < cutoffDate)
                    .ToListAsync();

                _dbContext.RemoveRange(oldRecords);
                await _dbContext.SaveChangesAsync();

                await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            }
        }
    }
}
