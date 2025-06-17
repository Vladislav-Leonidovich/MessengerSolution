using System.Text.Json;
using MassTransit;
using MessageService.Data;
using MessageService.Models;
using MessageService.Sagas.MessageDelivery.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Contracts;
using Shared.Outbox;

namespace MessageService.BackgroundServices
{
    public class MessageOutboxProcessorService : OutboxProcessorService<MessageDbContext>
    {
        private readonly ILogger<MessageOutboxProcessorService> _logger;

        public MessageOutboxProcessorService(
            OutboxProcessor<MessageDbContext> processor,
            OutboxOptions options,
            ILogger<MessageOutboxProcessorService> logger)
            : base(processor, options, logger)
        {
            _logger = logger;
        }

        protected override async Task PublishMessageToBusAsync(IBus bus, OutboxMessage message, CancellationToken cancellationToken)
        {
            try
            {
                var eventType = Type.GetType(message.EventType);

                if (eventType != null)
                {
                    var deserializedEvent = JsonSerializer.Deserialize(message.EventData, eventType);
                    if (deserializedEvent != null)
                    {
                        await bus.Publish(deserializedEvent, cancellationToken);
                        _logger.LogInformation("Опубліковано подію типу {EventType}", message.EventType);
                        return;
                    }
                }

                _logger.LogWarning("Не вдалося знайти або десеріалізувати тип: {EventType}", message.EventType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при публікації повідомлення типу {EventType}", message.EventType);
                throw;
            }
        }
    }
}
