using System.Text.Json;
using MassTransit;
using MessageService.Data;
using MessageService.Repositories;
using MessageService.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts;
using Shared.Sagas;

namespace MessageService.Sagas.DeleteAllMessages.Consumers
{
    public class DeleteChatMessagesCommandConsumer : IConsumer<DeleteChatMessagesCommand>
    {
        private readonly IMessageRepository _messageRepository;
        private readonly MessageDbContext _dbContext;
        private readonly ILogger<DeleteChatMessagesCommandConsumer> _logger;

        public DeleteChatMessagesCommandConsumer(
            IMessageRepository messageRepository,
            MessageDbContext dbContext,
            ILogger<DeleteChatMessagesCommandConsumer> logger)
        {
            _messageRepository = messageRepository;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<DeleteChatMessagesCommand> context)
        {
            try
            {
                _logger.LogInformation("Обробка команди DeleteChatMessagesCommand. " +
                    "ChatRoomId: {ChatRoomId}, CorrelationId: {CorrelationId}",
                    context.Message.ChatRoomId, context.Message.CorrelationId);

                // Перевірка ідемпотентності
                var processedEvent = await _dbContext.ProcessedEvents
                    .FirstOrDefaultAsync(e =>
                        e.EventId == context.Message.CorrelationId &&
                        e.EventType == "ChatMessagesDeleted");

                if (processedEvent != null)
                {
                    _logger.LogInformation("Повідомлення вже видалені для чату {ChatRoomId}",
                        context.Message.ChatRoomId);

                    // Виявляємо кількість видалених повідомлень (якщо збережена)
                    int deletedCount = 0;
                    if (!string.IsNullOrEmpty(processedEvent.EventData))
                    {
                        deletedCount = JsonSerializer.Deserialize<int>(processedEvent.EventData);
                    }

                    // Публікуємо подію для продовження саги
                    await context.Publish(new MessagesDeletedEvent
                    {
                        CorrelationId = context.Message.CorrelationId,
                        ChatRoomId = context.Message.ChatRoomId,
                        MessageCount = deletedCount,
                        DeletedAt = processedEvent.ProcessedAt
                    });

                    return;
                }

                var (success, actualDeletedCount) = await _messageRepository.DeleteAllMessagesByChatRoomIdAsync(
            context.Message.ChatRoomId);

                if (!success)
                {
                    throw new Exception("Помилка видалення повідомлень у репозиторії");
                }

                // Подальша обробка не потрібна, оскільки ProcessedEvent 
                // вже збережено в репозиторії

                _logger.LogInformation("Успішно видалено {Count} повідомлень з чату {ChatRoomId}",
                    actualDeletedCount, context.Message.ChatRoomId);

                // Публікуємо подію успішного видалення
                await context.Publish(new MessagesDeletedEvent
                {
                    CorrelationId = context.Message.CorrelationId,
                    ChatRoomId = context.Message.ChatRoomId,
                    MessageCount = actualDeletedCount,
                    DeletedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при видаленні повідомлень для чату {ChatRoomId}",
                    context.Message.ChatRoomId);

                // Публікуємо подію помилки
                await context.Publish(new ErrorEvent
                {
                    CorrelationId = context.Message.CorrelationId,
                    ErrorMessage = $"Помилка видалення повідомлень: {ex.Message}"
                });
            }
        }
    }
}
