using ChatService.Data;
using ChatService.Repositories.Interfaces;
using MassTransit;
using Shared.Consumers;
using Shared.Sagas;

namespace ChatService.Sagas.ChatCreation.Consumers
{
    public class CompensateChatCreationCommandConsumer : IdempotentConsumer<CompensateChatCreationCommand, ChatDbContext>
    {
        private readonly IChatRoomRepository _chatRoomRepository;
        private readonly ILogger<CompensateChatCreationCommandConsumer> _logger;

        public CompensateChatCreationCommandConsumer(
            IChatRoomRepository chatRoomRepository,
            ChatDbContext dbContext,
            ILogger<CompensateChatCreationCommandConsumer> logger)
            : base(dbContext, logger)
        {
            _chatRoomRepository = chatRoomRepository;
            _logger = logger;
        }

        protected override async Task ProcessEventAsync(ConsumeContext<CompensateChatCreationCommand> context)
        {
            var command = context.Message;

            _logger.LogInformation("Обробка команди CompensateChatCreationCommand. " +
                "ChatRoomId: {ChatRoomId}, CorrelationId: {CorrelationId}, Причина: {Reason}",
                command.ChatRoomId, command.CorrelationId, command.Reason);

            try
            {
                // Перевіряємо, чи існує чат з таким ID
                bool chatExists = await _chatRoomRepository.CheckIfChatExistsAsync(command.ChatRoomId);

                if (chatExists)
                {
                    // Видаляємо чат
                    bool success = await _chatRoomRepository.DeleteChatAsync(command.ChatRoomId);

                    if (success)
                    {
                        _logger.LogInformation("Чат {ChatRoomId} успішно видалено при компенсації",
                            command.ChatRoomId);
                    }
                    else
                    {
                        _logger.LogWarning("Не вдалося видалити чат {ChatRoomId} при компенсації",
                            command.ChatRoomId);
                    }
                }
                else
                {
                    _logger.LogInformation("Чат {ChatRoomId} не існує. Компенсація не потрібна.",
                        command.ChatRoomId);
                }

                // Публікуємо подію успішної компенсації
                await context.Publish(new ChatCreationCompensatedEvent
                {
                    CorrelationId = command.CorrelationId,
                    ChatRoomId = command.ChatRoomId,
                    Reason = command.Reason
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при компенсації створення чату {ChatRoomId}",
                    command.ChatRoomId);

                // Навіть при помилці компенсації, ми повинні завершити сагу, 
                // тому публікуємо подію компенсації з помилкою
                await context.Publish(new ChatCreationCompensatedEvent
                {
                    CorrelationId = command.CorrelationId,
                    ChatRoomId = command.ChatRoomId,
                    Reason = $"Помилка при компенсації: {ex.Message}. Початкова причина: {command.Reason}"
                });

                throw; // Перекидаємо виняток для обробки транзакції в базовому класі
            }
        }
    }
}
