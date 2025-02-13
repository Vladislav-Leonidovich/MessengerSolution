using MessageService.DTOs;
using MessageService.Models;

namespace MessageService.Services
{
    public interface IMessageService
    {
        // Зберігає повідомлення, передане клієнтом
        Task<Message> SendMessageAsync(SendMessageModel model);

        // Отримує список повідомлень для зазначеного чату з підтримкою пагінації
        Task<IEnumerable<Message>> GetMessagesAsync(int chatRoomId, int pageNumber, int pageSize);
    }
}
