using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shared.Outbox
{
    public class OutboxProcessor<TDbContext> where TDbContext : DbContext
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TDbContext> _logger;
        private readonly OutboxOptions _options;

        public OutboxProcessor(
            IServiceScopeFactory scopeFactory,
            ILogger<TDbContext> logger,
            OutboxOptions options)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _options = options;
        }

        public async Task ProcessMessagesAsync(
            Func<TDbContext, IQueryable<OutboxMessage>> getMessagesQuery,
            Func<IBus, OutboxMessage, CancellationToken, Task> publishMessage,
            CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
            var bus = scope.ServiceProvider.GetRequiredService<IBus>();

            // Отримання повідомлень для обробки
            var messages = await getMessagesQuery(dbContext)
                .Take(_options.BatchSize)
                .ToListAsync(cancellationToken);

            if (!messages.Any())
                return;

            _logger.LogInformation("Обробка {Count} повідомлень Outbox", messages.Count);

            foreach (var message in messages)
            {
                await ProcessSingleMessageAsync(dbContext, bus, message, publishMessage, cancellationToken);
            }
        }

        private async Task ProcessSingleMessageAsync(
            TDbContext dbContext,
            IBus bus,
            OutboxMessage message,
            Func<IBus, OutboxMessage, CancellationToken, Task> publishMessage,
            CancellationToken cancellationToken)
        {
            try
            {
                // Встановлення статусу Processing
                message.Status = OutboxMessageStatus.Processing;
                await dbContext.SaveChangesAsync(cancellationToken);

                // Публікація повідомлення
                await publishMessage(bus, message, cancellationToken);

                // Оновлення статусу повідомлення
                message.Status = OutboxMessageStatus.Processed;
                message.ProcessedAt = DateTime.UtcNow;
                message.Error = null;

                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleFailureAsync(dbContext, message, ex, cancellationToken);
            }
        }

        private async Task HandleFailureAsync(
            TDbContext dbContext,
            OutboxMessage message,
            Exception ex,
            CancellationToken cancellationToken)
        {
            message.RetryCount++;
            message.Error = ex.Message;

            if (message.RetryCount >= _options.MaxRetryCount)
            {
                message.Status = OutboxMessageStatus.Failed;
                _logger.LogError(ex, "Повідомлення Outbox {MessageId} не вдалося обробити після {RetryCount} спроб",
                    message.Id, message.RetryCount);
            }
            else
            {
                // Експоненційна затримка для повторних спроб
                int delayIndex = Math.Min(message.RetryCount - 1, _options.RetryDelays.Length - 1);
                message.NextRetryAt = DateTime.UtcNow.Add(_options.RetryDelays[delayIndex]);
                message.Status = OutboxMessageStatus.Pending;

                _logger.LogWarning(ex, "Помилка при обробці повідомлення Outbox {MessageId}. Наступна спроба {RetryCount}/{MaxRetry} через {Delay}",
                    message.Id, message.RetryCount, _options.MaxRetryCount, _options.RetryDelays[delayIndex]);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
