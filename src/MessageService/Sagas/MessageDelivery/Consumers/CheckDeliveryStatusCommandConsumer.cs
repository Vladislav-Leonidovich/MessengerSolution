using MassTransit;
using MessageService.Data;
using MessageService.Services.Interfaces;
using Shared.Sagas;

namespace MessageService.Sagas.MessageDelivery.Consumers
{
    public class CheckDeliveryStatusCommandConsumer : IConsumer<CheckDeliveryStatusCommand>
    {
        private readonly IChatGrpcClient _chatGrpcClient;
        private readonly ILogger<CheckDeliveryStatusCommandConsumer> _logger;
        private readonly MessageDeliverySagaDbContext _sagaDbContext;


        public CheckDeliveryStatusCommandConsumer(
            IChatGrpcClient chatGrpcClient,
            MessageDeliverySagaDbContext sagaDbContext,
            ILogger<CheckDeliveryStatusCommandConsumer> logger)
        {
            _chatGrpcClient = chatGrpcClient;
            _sagaDbContext = sagaDbContext;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<CheckDeliveryStatusCommand> context)
        {
            try
            {
                var correlationId = context.Message.CorrelationId;
                var messageId = context.Message.MessageId;

                _logger.LogInformation("Перевірка статусу доставки повідомлення {MessageId}, CorrelationId: {CorrelationId}",
                    messageId, correlationId);

                // Отримуємо учасників чату через gRPC
                var participants = await _chatGrpcClient.GetChatParticipantsAsync(
                    context.Message.ChatRoomId);

                // Видаляємо відправника зі списку учасників
                int senderUserId = context.Message.SenderUserId;
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
                    context.Message.MessageId);

                // У випадку помилки створюємо подію з негативним результатом
                await context.Publish(new DeliveryStatusCheckedEvent
                {
                    CorrelationId = context.Message.CorrelationId,
                    MessageId = context.Message.MessageId,
                    IsDeliveredToAll = false  // Змінюємо на false, щоб сага могла спробувати ще раз
                });
            }
        }
    }
}
