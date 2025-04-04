using Microsoft.AspNetCore.SignalR;
using MySqlX.XDevAPI;
using Shared.Contracts;

namespace MessageService.Hubs
{
    public class MessageHub : Hub
    {
        private readonly ILogger<MessageHub> _logger;

        public MessageHub(ILogger<MessageHub> logger)
        {
            _logger = logger;
        }

        // Метод для присоединения к группе
        public async Task JoinGroup(string groupName)
        {
            _logger.LogInformation("Клиент {ConnectionId} присоединяется к группе {GroupName}", Context.ConnectionId, groupName);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        // Метод для покидания группы
        public async Task LeaveGroup(string groupName)
        {
            _logger.LogInformation("Клиент {ConnectionId} покидает группу {GroupName}", Context.ConnectionId, groupName);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("Клиент подключился: {ConnectionId}", Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("Клиент отключился: {ConnectionId}", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
    }
}
