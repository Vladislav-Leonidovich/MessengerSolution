using MessageServiceDTOs;
using MessageService.Models;

namespace MessageService.Services
{
    public interface IMessageService
    {
        // Зберігає повідомлення, передане клієнтом
        Task<MessageDto> SendMessageAsync(SendMessageDto model);

        // Отримує список повідомлень для зазначеного чату з підтримкою пагінації
        Task<IEnumerable<MessageDto>> GetMessagesAsync(int chatRoomId, int startIndex, int count);
        Task<MessageDto> MarkMessageAsRead(int messageId);
        Task<MessageDto> GetLastMessagePreviewByChatRoomIdAsync(int chatRoomId);
        Task<bool> DeleteMessageAsync(int messageId);
        Task<bool> DeleteMessagesByChatRoomIdAsync(int chatRoomId);
        Task<ulong> GetMessagesCountByChatRoomIdAsync(int chatRoomId);
        Task<bool> IsAuthUserInChatRoomsAsync(int chatRoomId);
    }
}
