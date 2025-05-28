using System.Text.Json;
using MassTransit;
using MessageService.Data;
using MessageService.Repositories;
using MessageService.Repositories.Interfaces;
using MessageService.Sagas.DeleteAllMessages.Events;
using Microsoft.EntityFrameworkCore;
using Shared.Consumers;
using Shared.Contracts;

namespace MessageService.Sagas.DeleteAllMessages.Consumers
{
    public class DeleteChatMessagesCommandConsumer : IdempotentConsumer<DeleteChatMessagesCommand, MessageDbContext>
    {
        private readonly IMessageRepository _messageRepository;
        private readonly ILogger<DeleteChatMessagesCommandConsumer> _logger;

        public DeleteChatMessagesCommandConsumer(
            IMessageRepository messageRepository,
            MessageDbContext dbContext,
            ILogger<DeleteChatMessagesCommandConsumer> logger)
            : base(dbContext, logger)
        {
            _messageRepository = messageRepository;
            _logger = logger;
        }

        protected override async Task ProcessEventAsync(ConsumeContext<DeleteChatMessagesCommand> context)
        {
            var command = context.Message;

            _logger.LogInformation("Обробка команди DeleteChatMessagesCommand. " +
                "ChatRoomId: {ChatRoomId}, CorrelationId: {CorrelationId}",
                command.ChatRoomId, command.CorrelationId);

            try
            {
                var (success, actualDeletedCount) = await _messageRepository.DeleteAllMessagesByChatRoomIdAsync(
                    command.ChatRoomId);

                if (!success)
                {
                    throw new Exception("Помилка видалення повідомлень у репозиторії");
                }

                _logger.LogInformation("Успішно видалено {Count} повідомлень з чату {ChatRoomId}",
                    actualDeletedCount, command.ChatRoomId);

                // Публікуємо подію успішного видалення
                await context.Publish(new MessagesDeletedEvent
                {
                    CorrelationId = command.CorrelationId,
                    ChatRoomId = command.ChatRoomId,
                    MessageCount = actualDeletedCount,
                    DeletedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при видаленні повідомлень для чату {ChatRoomId}",
                    command.ChatRoomId);

                // Публікуємо подію помилки
                await context.Publish(new ErrorEvent
                {
                    CorrelationId = command.CorrelationId,
                    ErrorMessage = $"Помилка видалення повідомлень: {ex.Message}"
                });

                throw; // Перекидаємо виняток для базового класу
            }
        }
    }
}
