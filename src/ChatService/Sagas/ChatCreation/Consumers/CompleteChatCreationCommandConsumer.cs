using ChatService.Data;
using ChatService.Repositories.Interfaces;
using ChatService.Sagas.ChatCreation.Events;
using MassTransit;
using Shared.Consumers;

namespace ChatService.Sagas.ChatCreation.Consumers
{
    public class CompleteChatCreationCommandConsumer : IdempotentConsumer<CompleteChatCreationCommand, ChatDbContext>
    {
        private readonly IChatRoomRepository _chatRoomRepository;
        private readonly ILogger<CompleteChatCreationCommandConsumer> _logger;

        public CompleteChatCreationCommandConsumer(
            IChatRoomRepository chatRoomRepository,
            ChatDbContext dbContext,
            ILogger<CompleteChatCreationCommandConsumer> logger)
            : base(dbContext, logger)
        {
            _chatRoomRepository = chatRoomRepository;
            _logger = logger;
        }

        protected override async Task ProcessEventAsync(ConsumeContext<CompleteChatCreationCommand> context)
        {
            var command = context.Message;

            _logger.LogInformation("Обробка команди CompleteChatCreationCommand. " +
                "ChatRoomId: {ChatRoomId}, CorrelationId: {CorrelationId}",
                command.ChatRoomId, command.CorrelationId);

            try
            {
                // Перевіряємо, чи існує чат
                bool chatExists = await _chatRoomRepository.CheckIfChatExistsAsync(command.ChatRoomId);

                if (!chatExists)
                {
                    _logger.LogWarning("Чат {ChatRoomId} не існує при завершенні саги",
                        command.ChatRoomId);

                    // Викидаємо виключення, щоб обробити цю ситуацію
                    throw new Exception($"Чат {command.ChatRoomId} не знайдено при завершенні створення");
                }

                // Можливо, тут потрібно встановити статус чату як "активний" або виконати інші
                // дії для завершення процесу створення чату

                _logger.LogInformation("Створення чату {ChatRoomId} успішно завершено",
                    command.ChatRoomId);

                // Сага завершується успішно, тож нема потреби публікувати додаткові події,
                // оскільки сама сага має перейти в стан Completed
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при завершенні створення чату {ChatRoomId}",
                    command.ChatRoomId);

                // Публікуємо подію про невдачу
                await context.Publish(new ChatCreationFailedEvent
                {
                    CorrelationId = command.CorrelationId,
                    ChatRoomId = command.ChatRoomId,
                    Reason = $"Помилка при завершенні створення чату: {ex.Message}"
                });

                throw; // Перекидаємо виняток для обробки транзакції в базовому класі
            }
        }
    }
}
