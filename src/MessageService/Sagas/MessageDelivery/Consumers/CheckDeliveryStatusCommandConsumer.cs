using MassTransit;
using MessageService.Data;
using MessageService.Services.Interfaces;
using Shared.Consumers;
using MessageService.Sagas.MessageDelivery.Events;

namespace MessageService.Sagas.MessageDelivery.Consumers
{
    public class CheckDeliveryStatusCommandConsumer : IdempotentConsumer<CheckDeliveryStatusCommand, MessageDbContext>
    {
        private readonly IChatGrpcClient _chatGrpcClient;
        private readonly ILogger<CheckDeliveryStatusCommandConsumer> _logger;
        private readonly MessageDeliverySagaDbContext _sagaDbContext;

        public CheckDeliveryStatusCommandConsumer(
            IChatGrpcClient chatGrpcClient,
            MessageDeliverySagaDbContext sagaDbContext,
            MessageDbContext dbContext,
            ILogger<CheckDeliveryStatusCommandConsumer> logger)
            : base(dbContext, logger)
        {
            _chatGrpcClient = chatGrpcClient;
            _sagaDbContext = sagaDbContext;
            _logger = logger;
        }

        protected override async Task ProcessEventAsync(ConsumeContext<CheckDeliveryStatusCommand> context)
        {
            var command = context.Message;
            var correlationId = command.CorrelationId;
            var messageId = command.MessageId;

            _logger.LogInformation("Перевірка статусу доставки повідомлення {MessageId}, CorrelationId: {CorrelationId}",
                messageId, correlationId);

            try
            {
                // Отримуємо учасників чату через gRPC
                var participants = await _chatGrpcClient.GetChatParticipantsAsync(
                    command.ChatRoomId);

                // Видаляємо відправника зі списку учасників
                int senderUserId = command.SenderUserId;
                participants.Remove(senderUserId);

                // Отримуємо стан саги, щоб перевірити, кому вже доставлено
                var saga = await _sagaDbContext.MessageDeliverySagas
                    .FindAsync(correlationId);

                if (saga == null)
                {
                    _logger.LogWarning("Не знайдено сагу з CorrelationId {CorrelationId} для повідомлення {MessageId}",
                        correlationId, messageId);

                    // Якщо сага не знайдена, вважаємо, що повідомлення не доставлено
                    await context.Publish(new DeliveryStatusCheckedEvent
                    {
                        CorrelationId = correlationId,
                        MessageId = messageId,
                        IsDeliveredToAll = false
                    });

                    return;
                }

                // Перевіряємо, чи всі учасники отримали повідомлення
                bool allDelivered = participants.All(p => saga.DeliveredToUserIds.Contains(p));

                _logger.LogInformation("Статус доставки повідомлення {MessageId}: {DeliveredCount}/{TotalCount}, " +
                    "Всім доставлено: {AllDelivered}",
                    messageId, saga.DeliveredToUserIds.Count, participants.Count, allDelivered);

                // Публікуємо результат перевірки
                await context.Publish(new DeliveryStatusCheckedEvent
                {
                    CorrelationId = correlationId,
                    MessageId = messageId,
                    IsDeliveredToAll = allDelivered
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при перевірці статусу доставки повідомлення {MessageId}",
                    command.MessageId);

                // У випадку помилки створюємо подію з негативним результатом
                await context.Publish(new DeliveryStatusCheckedEvent
                {
                    CorrelationId = command.CorrelationId,
                    MessageId = command.MessageId,
                    IsDeliveredToAll = false  // Змінюємо на false, щоб сага могла спробувати ще раз
                });

                throw; // Перекидаємо виняток для базового класу
            }
        }
    }
}
