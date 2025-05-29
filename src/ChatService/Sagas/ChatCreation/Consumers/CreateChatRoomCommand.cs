using ChatService.Data;
using ChatService.Models;
using ChatService.Repositories.Interfaces;
using ChatService.Sagas.ChatCreation.Events;
using ChatService.Services.Interfaces;
using MassTransit;
using Shared.Consumers;
using Shared.DTOs.Chat;

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
                    existingOperation.Status == ChatOperationStatus.Completed)
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

                if (!command.MemberUserId.HasValue)
                {
                    _logger.LogError(
                        "Не вказано MemberUserId для команди CreateChatRoomCommand. " +
                        "ChatRoomId: {ChatRoomId}, CorrelationId: {CorrelationId}",
                        command.ChatRoomId, command.CorrelationId);
                    throw new ArgumentException("MemberUserId is required for creating a chat room.", nameof(command.MemberUserId));
                }

                var chatRoomType = await _chatRoomRepository.GetChatRoomTypeByIdAsync(command.ChatRoomId);
                var chatRoomId = command.ChatRoomId;

                switch (chatRoomType)
                {
                    case ChatRoomType.privateChat:
                        var createPrivateChatDto = new CreatePrivateChatRoomDto
                        {
                            UserId = command.MemberUserId.Value
                        };
                        var privateChatRoom = await _chatRoomRepository.CreatePrivateChatAsync(createPrivateChatDto, command.CreatorUserId);
                        chatRoomId = privateChatRoom.Id;
                        break;
                    case ChatRoomType.groupChat:
                        // Створюємо новий груповий чат
                        var createGroupChatDto = new CreateGroupChatRoomDto
                        {
                            Name = command.ChatName ?? "Group",
                            MemberIds = command.MemberIds
                        };
                        var groupChatRoom = await _chatRoomRepository.CreateGroupChatAsync(createGroupChatDto, command.CreatorUserId);
                        chatRoomId = groupChatRoom.Id;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(command.ChatRoomId),
                            "Невідомий тип чату");
                }

                _logger.LogInformation("Створено новий чат з ID {ChatRoomId}", chatRoomId);

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
