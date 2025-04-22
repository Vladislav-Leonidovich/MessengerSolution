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
    public abstract class IdempotentConsumer<TEvent> : IConsumer<TEvent> where TEvent : class
    {
        private readonly DbContext _dbContext;
        private readonly ILogger _logger;

        protected IdempotentConsumer(DbContext dbContext, ILogger logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<TEvent> context)
        {
            var eventId = GetEventId(context.Message);
            var eventType = typeof(TEvent).Name;

            // Проверка на устаревшие события
            if (IsEventExpired(context.Message))
            {
                _logger.LogWarning("Подія {EventType} з ID {EventId} застаріла і буде пропущена",
                    eventType, eventId);
                return;
            }

            // Проверка, не было ли уже обработано это событие
            var alreadyProcessed = await _dbContext.Set<ProcessedEvent>()
                .AnyAsync(pe => pe.EventId == eventId && pe.EventType == eventType);

            if (alreadyProcessed)
            {
                _logger.LogInformation("Подія {EventType} з ID {EventId} вже була оброблена. Пропускаємо.",
                    eventType, eventId);
                return;
            }

            try
            {
                // Выполнение бизнес-логики
                await ProcessEventAsync(context.Message);

                // Запись о том, что событие обработано
                _dbContext.Set<ProcessedEvent>().Add(new ProcessedEvent
                {
                    EventId = eventId,
                    EventType = eventType,
                    ProcessedAt = DateTime.UtcNow
                });

                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка під час обробки події {EventType} з ID {EventId}",
                    eventType, eventId);
                throw; // Пробрасываем исключение для повторной обработки сообщения
            }
        }

        protected abstract Task ProcessEventAsync(TEvent @event);

        protected virtual Guid GetEventId(TEvent @event)
        {
            // По умолчанию используем свойство EventId, если оно есть
            if (@event is MessageEventBase messageEvent)
            {
                return messageEvent.EventId;
            }

            // Иначе создаем детерминированный ID на основе содержимого события
            var json = JsonSerializer.Serialize(@event);
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(json));
            return new Guid(hash.Take(16).ToArray());
        }

        protected virtual bool IsEventExpired(TEvent @event)
        {
            // Если событие имеет свойство Timestamp
            if (@event is MessageEventBase messageEvent)
            {
                // Возвращаем true, если событие старше 1 дня
                return DateTime.UtcNow - messageEvent.Timestamp > TimeSpan.FromDays(1);
            }

            return false;
        }
    }
}
