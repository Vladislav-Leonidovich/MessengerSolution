using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.DTOs.Message;

namespace MauiClient.Services
{
    public interface IMessageService
    {
        Task<MessageDto?> SendMessageAsync(int chatRoomId, string content);
        Task<IEnumerable<MessageDto>> GetMessagesAsync(int chatRoomId, int startIndex, int count);
        Task<MessageDto?> MarkMessageAsRead(int messageId);
        Task<bool> DeleteMessageAsync(int messageId);
        Task<bool> DeleteMessagesByChatRoomId(int chatRoomId);
        Task<ulong?> GetMessagesCountByChatRoomIdAsync(int chatRoomId);
        Task StartConnectionAsync();
        Task JoinChatRoomAsync(int chatRoomId);
        Task LeaveChatRoomAsync(int chatRoomId);
        event Action<MessageDto> OnNewMessageReceived;
    }
}
