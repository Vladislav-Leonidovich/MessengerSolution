using MessageService.Models;
using MessageService.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace MessageService.Services
{
    public class OutboxEventPublisher : IEventPublisher
    {
        private readonly DbContext _dbContext;
        private readonly ILogger<OutboxEventPublisher> _logger;

        public OutboxEventPublisher(DbContext dbContext, ILogger<OutboxEventPublisher> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : class
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
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Set<OutboxMessage>().Add(outboxMessage);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Подія {EventType} з ID {EventId} збережена в Outbox",
                eventType, outboxMessage.Id);
        }
    }
}
