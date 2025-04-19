using MassTransit;
using MessageService.Data;
using MessageService.Hubs;
using MessageServiceDTOs;
using Microsoft.AspNetCore.SignalR;
using Shared.Consumers;
using Shared.Contracts;

namespace MessageService.Consumers
{
    public class MessageCreatedEventConsumer : IdempotentConsumer<MessageCreatedEvent>
    {
        private readonly IHubContext<MessageHub> _hubContext;

        public MessageCreatedEventConsumer(
            MessageDbContext dbContext,
            IHubContext<MessageHub> hubContext,
            ILogger<MessageCreatedEventConsumer> logger)
            : base(dbContext, logger)
        {
            _hubContext = hubContext;
        }

        protected override async Task ProcessEventAsync(MessageCreatedEvent @event)
        {
            // Конвертируем событие в DTO для отправки клиентам
            var messageDto = new MessageDto
            {
                Id = @event.Id,
                ChatRoomId = @event.ChatRoomId,
                ChatRoomType = @event.ChatRoomType,
                SenderUserId = @event.SenderUserId,
                Content = @event.Content,
                CreatedAt = @event.CreatedAt,
                IsRead = @event.IsRead,
                ReadAt = @event.ReadAt,
                IsEdited = @event.IsEdited,
                EditedAt = @event.EditedAt
            };

            // Отправка в группу
            string groupName = @event.ChatRoomId.ToString();
            await _hubContext.Clients.Group(groupName).SendAsync("ReceiveMessage", messageDto);
        }
    }
}
