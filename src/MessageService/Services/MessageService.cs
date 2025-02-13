using MessageService.Data;
using MessageService.DTOs;
using MessageService.Models;
using Microsoft.EntityFrameworkCore;

namespace MessageService.Services
{
    public class MessageService : IMessageService
    {
        private readonly MessageDbContext _dbContext;

        public MessageService(MessageDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Message> SendMessageAsync(SendMessageModel model)
        {
            // Створюємо об'єкт повідомлення на основі даних, отриманих від клієнта
            var message = new Message
            {
                ChatRoomId = model.ChatRoomId,
                SenderUserId = model.SenderUserId,
                Content = model.Content,
                CreatedAt = DateTime.UtcNow
            };

            // Додаємо повідомлення в базу даних
            _dbContext.Messages.Add(message);
            await _dbContext.SaveChangesAsync();

            return message;
        }

        public async Task<IEnumerable<Message>> GetMessagesAsync(int chatRoomId, int pageNumber, int pageSize)
        {
            // Витягуємо повідомлення для зазначеного чату із сортуванням за датою створення (від старих до нових)
            return await _dbContext.Messages
                .Where(m => m.ChatRoomId == chatRoomId)
                .OrderBy(m => m.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
    }
}
