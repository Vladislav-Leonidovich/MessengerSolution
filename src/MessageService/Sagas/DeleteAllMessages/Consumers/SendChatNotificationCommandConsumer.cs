using MassTransit;
using MessageService.Data;
using MessageService.Hubs;
using MessageService.Sagas.DeleteAllMessages.Events;
using MessageService.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Shared.Consumers;

namespace MessageService.Sagas.DeleteAllMessages.Consumers
{
    public class SendChatNotificationCommandConsumer : IdempotentConsumer<SendChatNotificationCommand, MessageDbContext>
    {
        private readonly IHubContext<MessageHub> _hubContext;
        private readonly IChatGrpcClient _chatGrpcClient;
        private readonly ILogger<SendChatNotificationCommandConsumer> _logger;

        public SendChatNotificationCommandConsumer(
            IHubContext<MessageHub> hubContext,
            IChatGrpcClient chatGrpcClient,
            MessageDbContext dbContext,
            ILogger<SendChatNotificationCommandConsumer> logger)
            : base(dbContext, logger)
        {
            _hubContext = hubContext;
            _chatGrpcClient = chatGrpcClient;
            _logger = logger;
        }

        protected override async Task ProcessEventAsync(ConsumeContext<SendChatNotificationCommand> context)
        {
            var command = context.Message;

            _logger.LogInformation("Надсилання сповіщень про видалення повідомлень. " +
                "ChatRoomId: {ChatRoomId}, CorrelationId: {CorrelationId}",
                command.ChatRoomId, command.CorrelationId);

            try
            {
                // Отримуємо учасників чату через gRPC
                var participants = await _chatGrpcClient.GetChatParticipantsAsync(
                    command.ChatRoomId);

                if (participants == null || !participants.Any())
                {
                    _logger.LogWarning("Не знайдено учасників для чату {ChatRoomId}",
                        command.ChatRoomId);

                    // Все одно публікуємо подію успіху з 0 отримувачів
                    await context.Publish(new NotificationsSentEvent
                    {
                        CorrelationId = command.CorrelationId,
                        RecipientCount = 0
                    });
                    return;
                }

                // Створюємо повідомлення для сповіщення
                var notification = new
                {
                    Type = "MessagesDeleted",
                    ChatRoomId = command.ChatRoomId,
                    Message = command.Message,
                    Timestamp = DateTime.UtcNow
                };

                // Надсилаємо сповіщення всім учасникам чату через SignalR
                var groupName = command.ChatRoomId.ToString();
                await _hubContext.Clients.Group(groupName)
                    .SendAsync("ChatMessagesDeleted", notification);

                // Додатково можна надіслати індивідуальні сповіщення
                /*foreach (var userId in participants)
                {
                    try
                    {
                        var userConnection = $"user_{userId}";
                        await _hubContext.Clients.Group(userConnection)
                            .SendAsync("ChatUpdate", new
                            {
                                Action = "MessagesDeleted",
                                ChatRoomId = command.ChatRoomId,
                                Details = command.Message
                            });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Не вдалося надіслати сповіщення користувачу {UserId}",
                            userId);
                        // Продовжуємо з іншими користувачами
                    }
                }*/

                _logger.LogInformation("Сповіщення надіслано {Count} учасникам чату {ChatRoomId}",
                    participants.Count, command.ChatRoomId);

                // Публікуємо подію успішного надсилання сповіщень
                await context.Publish(new NotificationsSentEvent
                {
                    CorrelationId = command.CorrelationId,
                    RecipientCount = participants.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при надсиланні сповіщень для чату {ChatRoomId}",
                    command.ChatRoomId);

                // Публікуємо подію помилки
                await context.Publish(new ErrorEvent
                {
                    CorrelationId = command.CorrelationId,
                    ErrorMessage = $"Помилка надсилання сповіщень: {ex.Message}"
                });

                throw; // Перекидаємо виняток для базового класу
            }
        }
    }
}
