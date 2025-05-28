using MassTransit;
using MessageService.Data;
using Shared.Consumers;
using MessageService.Sagas.MessageDelivery.Events;

namespace MessageService.Sagas.MessageDelivery.Consumers
{
    public class MessageDeliveredToUserEventConsumer : IdempotentConsumer<MessageDeliveredToUserEvent, MessageDbContext>
    {
        private readonly MessageDeliverySagaDbContext _sagaDbContext;
        private readonly ILogger<MessageDeliveredToUserEventConsumer> _logger;

        public MessageDeliveredToUserEventConsumer(
            MessageDeliverySagaDbContext sagaDbContext,
            MessageDbContext dbContext,
            ILogger<MessageDeliveredToUserEventConsumer> logger)
            : base(dbContext, logger)
        {
            _sagaDbContext = sagaDbContext;
            _logger = logger;
        }

        protected override async Task ProcessEventAsync(ConsumeContext<MessageDeliveredToUserEvent> context)
        {
            var message = context.Message;
            var correlationId = message.CorrelationId;
            var userId = message.UserId;

            _logger.LogInformation("Обробка підтвердження доставки повідомлення {MessageId} користувачу {UserId}, " +
                "CorrelationId: {CorrelationId}", message.MessageId, userId, correlationId);

            try
            {
                // Знаходимо стан саги
                var saga = await _sagaDbContext.MessageDeliverySagas
                    .FindAsync(correlationId);

                if (saga == null)
                {
                    _logger.LogWarning("Не знайдено сагу з CorrelationId {CorrelationId}", correlationId);
                    return;
                }

                // Додаємо користувача до списку отримувачів, якщо його там ще немає
                if (!saga.DeliveredToUserIds.Contains(userId))
                {
                    saga.DeliveredToUserIds.Add(userId);
                    await _sagaDbContext.SaveChangesAsync();

                    _logger.LogInformation("Додано користувача {UserId} до списку отримувачів повідомлення {MessageId}",
                        userId, message.MessageId);
                }
                else
                {
                    _logger.LogInformation("Користувач {UserId} вже є в списку отримувачів повідомлення {MessageId}",
                        userId, message.MessageId);
                }

                // Публікуємо подію для перевірки статусу доставки
                await context.Publish(new CheckDeliveryStatusCommand
                {
                    CorrelationId = correlationId,
                    MessageId = message.MessageId,
                    ChatRoomId = saga.ChatRoomId,
                    SenderUserId = saga.SenderUserId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при обробці підтвердження доставки повідомлення {MessageId} користувачу {UserId}",
                    message.MessageId, userId);
                throw; // Перекидаємо виняток для базового класу
            }
        }
    }
}
