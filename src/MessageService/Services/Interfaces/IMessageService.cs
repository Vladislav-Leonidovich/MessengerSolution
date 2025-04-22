using MessageServiceDTOs;
using MessageService.Models;
using Shared.Responses;

namespace MessageService.Services.Interfaces
{
    public interface IMessageService
    {
        // Зберігає повідомлення, передане клієнтом
        Task<ApiResponse<MessageDto>> SendMessageAsync(SendMessageDto model, int userId);
        Task<ApiResponse<MessageDto>> SendMessageViaSagaAsync(SendMessageDto model, int userId);

        // Отримує список повідомлень для зазначеного чату з підтримкою пагінації
        Task<ApiResponse<IEnumerable<MessageDto>>> GetMessagesAsync(int chatRoomId, int startIndex, int count, int userId);
        Task<ApiResponse<MessageDto>> MarkMessageAsRead(int messageId);
        Task<ApiResponse<MessageDto>> GetLastMessagePreviewByChatRoomIdAsync(int chatRoomId);
        Task<ApiResponse<bool>> DeleteMessageAsync(int messageId);
        Task<ApiResponse<bool>> DeleteMessagesByChatRoomIdAsync(int chatRoomId);
        Task<ApiResponse<int>> GetMessagesCountByChatRoomIdAsync(int chatRoomId);
        Task<bool> IsAuthUserInChatRoomsAsync(int chatRoomId);
    }
}
