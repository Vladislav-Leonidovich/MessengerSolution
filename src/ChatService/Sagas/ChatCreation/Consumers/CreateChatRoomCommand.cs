using ChatService.Data;
using ChatService.Models;
using ChatService.Repositories.Interfaces;
using ChatService.Services.Interfaces;
using ChatServiceDTOs.Chats;
using MassTransit;
using Shared.Consumers;
using Shared.Sagas;

namespace ChatService.Sagas.ChatCreation.Consumers
{
    public class CreateChatRoomCommandConsumer : IdempotentConsumer<CreateChatRoomCommand, ChatDbContext>
    {
        private readonly IChatRoomRepository _chatRoomRepository;
        private readonly IChatOperationService _operationService;
        private readonly ILogger<CreateChatRoomCommandConsumer> _logger;

        public CreateChatRoomCommandConsumer(
            IChatRoomRepository chatRoomRepository,
            IChatOperationService operationService,
            ChatDbContext dbContext,
            ILogger<CreateChatRoomCommandConsumer> logger)
            : base(dbContext, logger)
        {
            _chatRoomRepository = chatRoomRepository;
            _operationService = operationService;
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
                // Перевіряємо, чи операція вже існує
                var existingOperation = await _operationService.GetOperationAsync(command.CorrelationId);

                if (existingOperation != null &&
                    existingOperation.Status == ChatOperationStatus.Completed.ToString())
                {
                    _logger.LogInformation(
                        "Операція {CorrelationId} вже завершена",
                        command.CorrelationId);

                    // Публікуємо подію про успішне створення
                    await context.Publish(new ChatRoomCreatedEvent
                    {
                        CorrelationId = command.CorrelationId,
                        ChatRoomId = existingOperation.ChatRoomId
                    });

                    return;
                }

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
