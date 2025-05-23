using ChatService.Data;
using ChatService.Models;
using ChatService.Services.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;
using System.Text.Json;

namespace ChatService.Services
{
    public class OutboxEventPublisher : IEventPublisher
    {
        private readonly ChatDbContext _dbContext;
        private readonly ILogger<OutboxEventPublisher> _logger;

        public OutboxEventPublisher(ChatDbContext dbContext, ILogger<OutboxEventPublisher> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<OutboxMessage> CreateEventAsync<TEvent>(TEvent @event) where TEvent : class
        {
            var eventType = typeof(TEvent).Name;
            var eventData = JsonSerializer.Serialize(@event, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var outboxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                EventType = eventType,
                EventData = eventData,
                CreatedAt = DateTime.UtcNow,
                Status = OutboxMessageStatus.Pending
            };

            await _dbContext.OutboxMessages.AddAsync(outboxMessage);

            _logger.LogInformation("Подія {EventType} з ID {EventId} підготовлена для Outbox",
                eventType, outboxMessage.Id);

            return outboxMessage;
        }

        public async Task PublishInTransactionAsync<TEvent>(TEvent @event, IDbContextTransaction transaction)
            where TEvent : class
        {
            var outboxMessage = await CreateEventAsync(@event);

            _logger.LogInformation(
                "Подія {EventType} з ID {EventId} додана до Outbox в рамках транзакції",
                outboxMessage.EventType, outboxMessage.Id);
        }

        public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : class
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                await PublishInTransactionAsync(@event, transaction);
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Подія {EventType} успішно збережена в Outbox",
                    typeof(TEvent).Name);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Помилка при збереженні події {EventType} в Outbox",
                    typeof(TEvent).Name);
                throw;
            }
        }
    }
}
