using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessageServiceDTOs;

namespace MauiClient.Services
{
    public interface IMessageService
    {
        Task<MessageDto?> SendMessageAsync(SendMessageDto model);
        Task<IEnumerable<MessageDto>> GetMessagesAsync(int chatRoomId, int startIndex, int count);
        Task<MessageDto?> MarkMessageAsRead(int messageId);
        Task<bool> DeleteMessageAsync(int messageId);
        Task<bool> DeleteMessagesByChatRoomId(int chatRoomId);
        Task<ulong> GetMessagesCountByChatRoomIdAsync(int chatRoomId);
        Task StartConnectionAsync();
        event Action<MessageDto> OnNewMessageReceived;
    }
}
