using ChatService.Data;
using ChatService.Repositories.Interfaces;
using ChatServiceDTOs.Chats;
using MassTransit;
using Shared.Consumers;
using Shared.Sagas;

namespace ChatService.Sagas.ChatCreation.Consumers
{
    public class CreateChatRoomCommandConsumer : IdempotentConsumer<CreateChatRoomCommand, ChatDbContext>
    {
        private readonly IChatRoomRepository _chatRoomRepository;
        private readonly ILogger<CreateChatRoomCommandConsumer> _logger;

        public CreateChatRoomCommandConsumer(
            IChatRoomRepository chatRoomRepository,
            ChatDbContext dbContext,
            ILogger<CreateChatRoomCommandConsumer> logger)
            : base(dbContext, logger)
        {
            _chatRoomRepository = chatRoomRepository;
            _logger = logger;
        }

        protected override async Task ProcessEventAsync(ConsumeContext<CreateChatRoomCommand> context)
        {
            var command = context.Message;

            _logger.LogInformation("Обробка команди CreateChatRoomCommand. " +
                "ChatRoomId: {ChatRoomId}, CorrelationId: {CorrelationId}",
                command.ChatRoomId, command.CorrelationId);

            try
            {
                // Перевіряємо, чи вже існує чат з таким ID
                bool chatExists = false;

                // Використовуємо існуючий ID чату або створюємо новий
                if (command.ChatRoomId != 0)
                {
                    // Перевіряємо, чи існує такий чат
                    chatExists = await _chatRoomRepository.CheckIfChatExistsAsync(command.ChatRoomId);

                    if (chatExists)
                    {
                        _logger.LogInformation("Чат з ID {ChatRoomId} вже існує. Продовжуємо сагу.",
                            command.ChatRoomId);
                    }
                }

                if (!chatExists)
                {
                    // Створюємо новий груповий чат
                    var createGroupChatDto = new CreateGroupChatRoomDto
                    {
                        Name = "Новий чат", // Можна налаштувати з параметрів команди
                        MemberIds = command.MemberIds
                    };

                    // Викликаємо репозиторій для створення чату
                    var chatRoom = await _chatRoomRepository.CreateGroupChatAsync(
                        createGroupChatDto, command.CreatorUserId);

                    _logger.LogInformation("Створено новий чат з ID {ChatRoomId}", chatRoom.Id);
                }

                // Публікуємо подію успішного створення чату 
                // (використовуємо context, який приходить з параметра)
                await context.Publish(new ChatRoomCreatedEvent
                {
                    CorrelationId = command.CorrelationId,
                    ChatRoomId = command.ChatRoomId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при створенні чату {ChatRoomId}", command.ChatRoomId);

                // Публікуємо подію про помилку
                await context.Publish(new ChatCreationFailedEvent
                {
                    CorrelationId = command.CorrelationId,
                    ChatRoomId = command.ChatRoomId,
                    Reason = $"Помилка створення чату: {ex.Message}"
                });

                throw; // Перекидаємо виняток для обробки транзакції в базовому класі
            }
        }
    }
}
