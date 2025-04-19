using MassTransit;
using MessageService.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts;
using MessageService.Services.Interfaces;

namespace ChatService.Consumers
{
    public class ChatDeletedEventConsumer : IConsumer<ChatDeletedEvent>
    {
        private readonly MessageDbContext _context;
        private readonly IMessageService _messageService;

        public ChatDeletedEventConsumer(MessageDbContext context, IMessageService messageService)
        {
            _context = context;
            _messageService = messageService;
        }

        public async Task Consume(ConsumeContext<ChatDeletedEvent> context)
        {
            var chatRoomId = context.Message.ChatRoomId;
            var IsAuthUser = await _messageService.IsAuthUserInChatRoomsAsync(chatRoomId);
            if (IsAuthUser)
            {
                await DeleteMessagesByChatRoomId(chatRoomId);
            }
        }

        private async Task DeleteMessagesByChatRoomId(int chatRoomId)
        {
            var messages = await _context.Messages
                .Where(m => m.ChatRoomId == chatRoomId)
                .ToListAsync();


            if (messages.Any())
            {
                _context.Messages.RemoveRange(messages);
                await _context.SaveChangesAsync();
            }
        }
    }
}
