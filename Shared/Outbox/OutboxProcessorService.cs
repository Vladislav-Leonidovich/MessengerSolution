using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Shared.Outbox
{
    // Shared/Outbox/OutboxProcessorService.cs
    public abstract class OutboxProcessorService<TDbContext> : BackgroundService
        where TDbContext : DbContext
    {
        private readonly OutboxProcessor<TDbContext> _processor;
        private readonly OutboxOptions _options;
        private readonly ILogger _logger;

        protected OutboxProcessorService(
            OutboxProcessor<TDbContext> processor,
            OutboxOptions options,
            ILogger logger)
        {
            _processor = processor;
            _options = options;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Outbox Processor Service запущений");

            using var timer = new PeriodicTimer(_options.PollingInterval);

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

        protected virtual async Task ProcessOutboxMessagesAsync(CancellationToken stoppingToken)
        {
            await _processor.ProcessMessagesAsync(
                GetMessagesQuery,
                PublishMessageToBusAsync,
                stoppingToken);
        }

        protected virtual IQueryable<OutboxMessage> GetMessagesQuery(TDbContext dbContext)
        {
            return dbContext.Set<OutboxMessage>()
                .Where(m => m.Status == OutboxMessageStatus.Pending &&
                      (m.NextRetryAt == null || m.NextRetryAt <= DateTime.UtcNow) &&
                       m.RetryCount < _options.MaxRetryCount)
                .OrderBy(m => m.CreatedAt);
        }

        protected abstract Task PublishMessageToBusAsync(IBus bus, OutboxMessage message, CancellationToken cancellationToken);
    }
}
