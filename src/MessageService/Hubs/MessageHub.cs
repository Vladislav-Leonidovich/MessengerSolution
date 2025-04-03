using Microsoft.AspNetCore.SignalR;
using MySqlX.XDevAPI;
using Shared.Contracts;

namespace MessageService.Hubs
{
    public class MessageHub : Hub
    {
        // Метод для відправки повідомлень клієнтам
        public async Task SendMessageToClient(MessageCreatedEvent message)
        {
            await Clients.Group(message.ChatRoomId.ToString()).SendAsync("ReceiveMessage", message);
        }
    }
}
