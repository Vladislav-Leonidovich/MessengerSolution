using MassTransit;
using MessageService.Repositories.Interfaces;
using MessageServiceDTOs;
using Shared.Sagas;

namespace MessageService.Sagas.MessageDelivery.Consumers
{
    public class SaveMessageCommandConsumer : IConsumer<SaveMessageCommand>
    {
        private readonly IMessageRepository _messageRepository;
        private readonly ILogger<SaveMessageCommandConsumer> _logger;

        public SaveMessageCommandConsumer(
            IMessageRepository messageRepository,
            ILogger<SaveMessageCommandConsumer> logger)
        {
            _messageRepository = messageRepository;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<SaveMessageCommand> context)
        {
            try
            {
                _logger.LogInformation("Обробка команди SaveMessageCommand. MessageId: {MessageId}",
                    context.Message.MessageId);

                // Перетворення команди на DTO для збереження
                var sendMessageDto = new SendMessageDto
                {
                    ChatRoomId = context.Message.ChatRoomId,
                    ChatRoomType = context.Message.ChatRoomType,
                    Content = context.Message.Content
                };

                // Збереження повідомлення в базу даних
                var messageDto = await _messageRepository.CreateMessageWithEventAsync(sendMessageDto, context.Message.SenderUserId);

                // Публікація події успішного збереження
                await context.Publish(new MessageSavedEvent
                {
                    CorrelationId = context.Message.CorrelationId,
                    MessageId = messageDto.Id,
                    EncryptedContent = messageDto.Content // Вже зашифрований вміст
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при збереженні повідомлення {MessageId}", context.Message.MessageId);

                await context.Publish(new MessageDeliveryFailedEvent
                {
                    CorrelationId = context.Message.CorrelationId,
                    MessageId = context.Message.MessageId,
                    Reason = $"Помилка збереження повідомлення: {ex.Message}"
                });
            }
        }
    }
}
