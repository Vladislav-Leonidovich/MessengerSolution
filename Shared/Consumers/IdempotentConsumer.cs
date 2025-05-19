using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Contracts;

namespace Shared.Consumers
{
    public abstract class IdempotentConsumer<TEvent, TDbContext> : IConsumer<TEvent>
        where TEvent : class
        where TDbContext : DbContext
    {
        private readonly TDbContext _dbContext;
        private readonly ILogger _logger;

        protected IdempotentConsumer(TDbContext dbContext, ILogger logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<TEvent> context)
        {
            var eventId = GetEventId(context.Message);
            var eventType = typeof(TEvent).Name;

            // Перевірка на застарілі події
            if (IsEventExpired(context.Message))
            {
                _logger.LogWarning("Подія {EventType} з ID {EventId} застаріла і буде пропущена",
                    eventType, eventId);
                return;
            }

            // Перевірка, чи не було вже оброблено цю подію
            var alreadyProcessed = await _dbContext.Set<ProcessedEvent>()
                .AnyAsync(pe => pe.EventId == eventId && pe.EventType == eventType);

            if (alreadyProcessed)
            {
                _logger.LogInformation("Подія {EventType} з ID {EventId} вже була оброблена. Пропускаємо.",
                    eventType, eventId);
                return;
            }

            // Використовуємо транзакцію для обробки
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                // Виконання бізнес-логіки
                await ProcessEventAsync(context);

                // Запис про те, що подія оброблена
                _dbContext.Set<ProcessedEvent>().Add(new ProcessedEvent
                {
                    EventId = eventId,
                    EventType = eventType,
                    ProcessedAt = DateTime.UtcNow
                });

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Помилка під час обробки події {EventType} з ID {EventId}",
                    eventType, eventId);
                throw; // Пробрасываем исключение для повторной обработки сообщения
            }
        }

        protected abstract Task ProcessEventAsync(ConsumeContext<TEvent> context);

        protected virtual Guid GetEventId(TEvent @event)
        {
            // Основна реалізація GetEventId як у поточному коді
            // По замовчуванню використовуємо властивість CorrelationId, якщо вона є
            var correlationIdProperty = @event.GetType().GetProperty("CorrelationId");
            if (correlationIdProperty != null && correlationIdProperty.PropertyType == typeof(Guid))
            {
                return (Guid)correlationIdProperty.GetValue(@event);
            }

            // Інакше створюємо детермінований ID на основі вмісту події
            var json = JsonSerializer.Serialize(@event);
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(json));
            return new Guid(hash.Take(16).ToArray());
        }

        protected virtual bool IsEventExpired(TEvent @event)
        {
            // Отримуємо властивість Timestamp, якщо вона є
            var timestampProperty = @event.GetType().GetProperty("Timestamp");
            if (timestampProperty != null && timestampProperty.PropertyType == typeof(DateTime))
            {
                var timestamp = (DateTime)timestampProperty.GetValue(@event);
                // Повертаємо true, якщо подія старша за 1 день
                return DateTime.UtcNow - timestamp > TimeSpan.FromDays(1);
            }

            return false;
        }
    }
}
