using MassTransit;
using MessageService.Hubs;
using MessageServiceDTOs;
using Microsoft.AspNetCore.SignalR;
using Shared.Contracts;

namespace MessageService.Consumers
{
    public class MessageCreatedEventConsumer : IConsumer<MessageCreatedEvent>
    {
        private readonly IHubContext<MessageHub> _hubContext;
        private readonly ILogger<MessageCreatedEventConsumer> _logger;

        public MessageCreatedEventConsumer(IHubContext<MessageHub> hubContext, ILogger<MessageCreatedEventConsumer> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<MessageCreatedEvent> context)
        {
            var message = context.Message;
            _logger.LogInformation("Получено событие MessageCreatedEvent для чата {ChatRoomId}", message.ChatRoomId);

            try
            {
                // Конвертируем событие в DTO для отправки клиентам
                var messageDto = new MessageDto
                {
                    Id = message.Id,
                    ChatRoomId = message.ChatRoomId,
                    ChatRoomType = message.ChatRoomType,
                    SenderUserId = message.SenderUserId,
                    Content = message.Content,
                    CreatedAt = message.CreatedAt,
                    IsRead = message.IsRead,
                    ReadAt = message.ReadAt,
                    IsEdited = message.IsEdited,
                    EditedAt = message.EditedAt
                };

                // ВАЖНО: группа должна точно соответствовать ID чата
                string groupName = message.ChatRoomId.ToString();

                _logger.LogInformation("Отправка сообщения в группу {GroupName}", groupName);

                // Отправка в группу
                await _hubContext.Clients
                    .Group(groupName)
                    .SendAsync("ReceiveMessage", messageDto);

                _logger.LogInformation("Сообщение успешно отправлено в группу {GroupName}", groupName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отправки сообщения в группу {ChatRoomId}", message.ChatRoomId);
            }
        }
    }
}
