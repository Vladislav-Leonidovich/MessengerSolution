using MessageServiceDTOs;
using MessageService.Models;

namespace MessageService.Services
{
    public interface IMessageService
    {
        // Зберігає повідомлення, передане клієнтом
        Task<MessageDto> SendMessageAsync(SendMessageDto model);

        // Отримує список повідомлень для зазначеного чату з підтримкою пагінації
        Task<IEnumerable<MessageDto>> GetMessagesAsync(int chatRoomId, int pageNumber, int pageSize);
        Task<MessageDto> MarkMessageAsRead(int messageId);
    }
}
