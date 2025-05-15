using MassTransit;
using MessageService.Hubs;
using MessageService.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Shared.Sagas;

namespace MessageService.Sagas.DeleteAllMessages.Consumers
{
    public class SendChatNotificationCommandConsumer : IConsumer<SendChatNotificationCommand>
    {
        private readonly IHubContext<MessageHub> _hubContext;
        private readonly IChatGrpcClient _chatGrpcClient;
        private readonly ILogger<SendChatNotificationCommandConsumer> _logger;

        public SendChatNotificationCommandConsumer(
            IHubContext<MessageHub> hubContext,
            IChatGrpcClient chatGrpcClient,
            ILogger<SendChatNotificationCommandConsumer> logger)
        {
            _hubContext = hubContext;
            _chatGrpcClient = chatGrpcClient;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<SendChatNotificationCommand> context)
        {
            try
            {
                _logger.LogInformation("Надсилання сповіщень про видалення повідомлень. " +
                    "ChatRoomId: {ChatRoomId}, CorrelationId: {CorrelationId}",
                    context.Message.ChatRoomId, context.Message.CorrelationId);

                // Отримуємо учасників чату через gRPC
                var participants = await _chatGrpcClient.GetChatParticipantsAsync(
                    context.Message.ChatRoomId,
                    MessageServiceDTOs.ChatRoomType.privateChat); // Або визначте тип динамічно

                if (participants == null || !participants.Any())
                {
                    _logger.LogWarning("Не знайдено учасників для чату {ChatRoomId}",
                        context.Message.ChatRoomId);

                    // Все одно публікуємо подію успіху з 0 отримувачів
                    await context.Publish(new NotificationsSentEvent
                    {
                        CorrelationId = context.Message.CorrelationId,
                        RecipientCount = 0
                    });
                    return;
                }

                // Створюємо повідомлення для сповіщення
                var notification = new
                {
                    Type = "MessagesDeleted",
                    ChatRoomId = context.Message.ChatRoomId,
                    Message = context.Message.Message,
                    Timestamp = DateTime.UtcNow
                };

                // Надсилаємо сповіщення всім учасникам чату через SignalR
                var groupName = context.Message.ChatRoomId.ToString();
                await _hubContext.Clients.Group(groupName)
                    .SendAsync("ChatMessagesDeleted", notification);

                // Додатково можна надіслати індивідуальні сповіщення
                foreach (var userId in participants)
                {
                    try
                    {
                        var userConnection = $"user_{userId}";
                        await _hubContext.Clients.Group(userConnection)
                            .SendAsync("ChatUpdate", new
                            {
                                Action = "MessagesDeleted",
                                ChatRoomId = context.Message.ChatRoomId,
                                Details = context.Message.Message
                            });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Не вдалося надіслати сповіщення користувачу {UserId}",
                            userId);
                        // Продовжуємо з іншими користувачами
                    }
                }

                _logger.LogInformation("Сповіщення надіслано {Count} учасникам чату {ChatRoomId}",
                    participants.Count, context.Message.ChatRoomId);

                // Публікуємо подію успішного надсилання сповіщень
                await context.Publish(new NotificationsSentEvent
                {
                    CorrelationId = context.Message.CorrelationId,
                    RecipientCount = participants.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при надсиланні сповіщень для чату {ChatRoomId}",
                    context.Message.ChatRoomId);

                // Публікуємо подію помилки
                await context.Publish(new ErrorEvent
                {
                    CorrelationId = context.Message.CorrelationId,
                    ErrorMessage = $"Помилка надсилання сповіщень: {ex.Message}"
                });
            }
        }
    }
}
