using MassTransit;
using MessageService.Data;
using MessageService.Repositories.Interfaces;
using Shared.DTOs.Message;
using Shared.Consumers;
using MessageService.Sagas.MessageDelivery.Events;

namespace MessageService.Sagas.MessageDelivery.Consumers
{
    public class SaveMessageCommandConsumer : IdempotentConsumer<SaveMessageCommand, MessageDbContext>
    {
        private readonly IMessageRepository _messageRepository;
        private readonly ILogger<SaveMessageCommandConsumer> _logger;

        public SaveMessageCommandConsumer(
            IMessageRepository messageRepository,
            MessageDbContext dbContext,
            ILogger<SaveMessageCommandConsumer> logger)
            : base(dbContext, logger)
        {
            _messageRepository = messageRepository;
            _logger = logger;
        }

        protected override async Task ProcessEventAsync(ConsumeContext<SaveMessageCommand> context)
        {
            var command = context.Message;

            _logger.LogInformation("Обробка команди SaveMessageCommand. MessageId: {MessageId}, CorrelationId: {CorrelationId}",
                command.MessageId, command.CorrelationId);

            try
            {
                // Перевіряємо, чи вже існує повідомлення з таким CorrelationId
                var existingMessage = await _messageRepository.FindMessageByCorrelationIdAsync(command.CorrelationId);

                if (existingMessage != null)
                {
                    // Якщо повідомлення вже існує, публікуємо подію про успішне збереження
                    _logger.LogInformation("Знайдено існуюче повідомлення з ID {MessageId} для CorrelationId {CorrelationId}",
                        existingMessage.Id, command.CorrelationId);

                    await context.Publish(new MessageSavedEvent
                    {
                        CorrelationId = command.CorrelationId,
                        MessageId = existingMessage.Id,
                        EncryptedContent = existingMessage.Content
                    });

                    return;
                }

                // Перетворення команди на DTO для збереження
                var sendMessageDto = new SendMessageDto
                {
                    ChatRoomId = command.ChatRoomId,
                    Content = command.Content
                };

                // Збереження повідомлення в базу даних
                var messageDto = await _messageRepository.CreateMessageForSagaAsync(
                    sendMessageDto,
                    command.SenderUserId,
                    command.CorrelationId);

                // Публікація події успішного збереження
                await context.Publish(new MessageSavedEvent
                {
                    CorrelationId = command.CorrelationId,
                    MessageId = messageDto.Id,
                    EncryptedContent = messageDto.Content // Вже зашифрований вміст
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при збереженні повідомлення {MessageId}", command.MessageId);

                await context.Publish(new MessageDeliveryFailedEvent
                {
                    CorrelationId = command.CorrelationId,
                    MessageId = command.MessageId,
                    Reason = $"Помилка збереження повідомлення: {ex.Message}"
                });

                throw; // Перекидаємо виняток для базового класу, який відкотить транзакцію
            }
        }
    }
}
