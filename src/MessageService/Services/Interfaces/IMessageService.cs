using MessageService.Models;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Shared.DTOs.Message;
using Shared.DTOs.Responses;

namespace MessageService.Services.Interfaces
{
    public interface IMessageService
    {
        // Зберігає повідомлення, передане клієнтом
        Task<ApiResponse<Task>> SendMessageViaSagaAsync(string content, int userId, int chatRoomId);
        Task<ApiResponse<MessageDto>> SendMessageAsync(string content, int userId, int chatRoomId);

        // Отримує список повідомлень для зазначеного чату з підтримкою пагінації
        Task<ApiResponse<IEnumerable<MessageDto>>> GetMessagesAsync(int chatRoomId, int userId, int startIndex, int count);
        Task<ApiResponse<MessageDto>> MarkMessageAsRead(int messageId, int userId);
        Task<ApiResponse<MessageDto>> GetLastMessagePreviewByChatRoomIdAsync(int chatRoomId, int userId);
        Task<ApiResponse<Dictionary<int, MessageDto>>> GetLastMessagesBatchAsync(IEnumerable<int> chatRoomIds, int userId);
        Task<ApiResponse<bool>> DeleteMessageAsync(int messageId, int userId);
        Task<ApiResponse<bool>> DeleteMessagesByChatRoomIdAsync(int chatRoomId, int userId);
        Task<ApiResponse<int>> GetMessagesCountByChatRoomIdAsync(int chatRoomId, int userId);
        Task<ApiResponse<bool>> ConfirmMessageDeliveryAsync(int messageId, int userId);
    }
}
