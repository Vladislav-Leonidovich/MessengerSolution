using MessageServiceDTOs;
using MessageService.Models;
using Shared.Responses;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace MessageService.Services.Interfaces
{
    public interface IMessageService
    {
        // Зберігає повідомлення, передане клієнтом
        Task<ApiResponse<MessageDto>> SendMessageViaSagaAsync(SendMessageDto model, int userId);

        // Отримує список повідомлень для зазначеного чату з підтримкою пагінації
        Task<ApiResponse<IEnumerable<MessageDto>>> GetMessagesAsync(int chatRoomId, int userId, int startIndex, int count);
        Task<ApiResponse<MessageDto>> MarkMessageAsRead(int messageId, int userId);
        Task<ApiResponse<MessageDto>> GetLastMessagePreviewByChatRoomIdAsync(int chatRoomId, int userId);
        Task<ApiResponse<bool>> DeleteMessageAsync(int messageId, int userId);
        Task<ApiResponse<bool>> DeleteMessagesByChatRoomIdAsync(int chatRoomId, int userId);
        Task<ApiResponse<int>> GetMessagesCountByChatRoomIdAsync(int chatRoomId, int userId);
        Task<ApiResponse<bool>> ConfirmMessageDeliveryAsync(int messageId, int userId);
    }
}
