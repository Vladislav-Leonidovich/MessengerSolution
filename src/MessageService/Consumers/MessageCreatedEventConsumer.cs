using MassTransit;
using MessageService.Hubs;
using Microsoft.AspNetCore.SignalR;
using Shared.Contracts;

namespace MessageService.Consumers
{
    public class MessageCreatedEventConsumer : IConsumer<MessageCreatedEvent>
    {
        private readonly IHubContext<MessageHub> _hubContext;

        public MessageCreatedEventConsumer(IHubContext<MessageHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task Consume(ConsumeContext<MessageCreatedEvent> context)
        {
            var message = context.Message;

            // Через HubContext надсилаємо повідомлення всім клієнтам в групі
            await _hubContext.Clients
                .Group(message.ChatRoomId.ToString())
                .SendAsync("ReceiveMessage", message);
        }
    }
}
