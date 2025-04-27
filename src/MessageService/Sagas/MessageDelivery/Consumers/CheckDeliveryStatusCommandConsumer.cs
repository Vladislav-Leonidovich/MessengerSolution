using MassTransit;
using MessageService.Services.Interfaces;
using Shared.Sagas;

namespace MessageService.Sagas.MessageDelivery.Consumers
{
    public class CheckDeliveryStatusCommandConsumer : IConsumer<CheckDeliveryStatusCommand>
    {
        private readonly IChatGrpcClient _chatGrpcClient;
        private readonly ILogger<CheckDeliveryStatusCommandConsumer> _logger;
        private readonly ISagaRepository<MessageDeliverySagaState> _repository;

        public CheckDeliveryStatusCommandConsumer(
            IChatGrpcClient chatGrpcClient,
            ILogger<CheckDeliveryStatusCommandConsumer> logger,
            ISagaRepository<MessageDeliverySagaState> repository)
        {
            _chatGrpcClient = chatGrpcClient;
            _logger = logger;
            _repository = repository;
        }

        public async Task Consume(ConsumeContext<CheckDeliveryStatusCommand> context)
        {
            try
            {
                _logger.LogInformation("Перевірка статусу доставки повідомлення {MessageId}",
                    context.Message.MessageId);

                // Отримуємо учасників чату
                var participants = await _chatGrpcClient.GetChatParticipantsAsync(
                    context.Message.ChatRoomId, context.Message.ChatRoomType);

                // Для визначення відправника потрібно знати його ідентифікатор
                // Оскільки ми не можемо отримати sagaInstance, будемо використовувати дані з самої команди
                int senderUserId = context.Message.SenderUserId;

                // Видаляємо відправника зі списку
                participants.Remove(senderUserId);

                // Отримуємо інформацію про вже доставлені повідомлення
                // через запит до бази даних або через інший сервіс
                var deliveredToUserIds = await _chatGrpcClient.GetMessageDeliveryStatusAsync(
                    context.Message.MessageId, context.Message.ChatRoomId);

                // Перевіряємо, чи всі учасники отримали повідомлення
                bool allDelivered = participants.All(p => deliveredToUserIds.Contains(p));

                // Публікуємо результат перевірки
                await context.Publish(new DeliveryStatusCheckedEvent
                {
                    CorrelationId = context.Message.CorrelationId,
                    MessageId = context.Message.MessageId,
                    IsDeliveredToAll = allDelivered
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при перевірці статусу доставки повідомлення {MessageId}",
                    context.Message.MessageId);

                // У випадку помилки вважаємо, що доставка завершена (щоб сага не застрягла)
                await context.Publish(new DeliveryStatusCheckedEvent
                {
                    CorrelationId = context.Message.CorrelationId,
                    MessageId = context.Message.MessageId,
                    IsDeliveredToAll = true
                });
            }
        }
    }
}
