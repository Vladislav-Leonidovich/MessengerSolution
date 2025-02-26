using MassTransit;
using MessageService.Data;
using MessageService.DTOs;
using MessageService.Models;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts;
using EncryptionServiceDTOs;

namespace MessageService.Services
{
    public class MessageService : IMessageService
    {
        private readonly MessageDbContext _dbContext;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly HttpClient _encryptionClient;

        public MessageService(MessageDbContext dbContext, IPublishEndpoint publishEndpoint, HttpClient encryptionClient)
        {
            _dbContext = dbContext;
            _publishEndpoint = publishEndpoint;
            _encryptionClient = encryptionClient;
        }

        public async Task<Message> SendMessageAsync(SendMessageDto model)
        {
            // Викликаємо API EncryptionService для шифрування
            var encryptionRequest = new EncryptionRequest
            {
                PlainText = model.Content
            };

            var encryptionResponse = await _encryptionClient.PostAsJsonAsync("api/encryption/encrypt", encryptionRequest);
            if (!encryptionResponse.IsSuccessStatusCode)
            {
                throw new Exception("Не вдалося зашифрувати повідомлення.");
            }
            var encryptionResult = await encryptionResponse.Content.ReadFromJsonAsync<string>();
            string encryptedContent = encryptionResult ?? throw new Exception("Немає зашифрованого вмісту.");

            // Створюємо об'єкт повідомлення на основі даних, отриманих від клієнта
            var message = new Message
            {
                ChatRoomId = model.ChatRoomId,
                SenderUserId = model.SenderUserId,
                Content = encryptedContent,
                CreatedAt = DateTime.UtcNow
            };

            // Додаємо повідомлення в базу даних
            _dbContext.Messages.Add(message);
            await _dbContext.SaveChangesAsync();

            // Публікуємо подію MessageCreatedEvent
            await _publishEndpoint.Publish(new MessageCreatedEvent
            {
                MessageId = message.Id,
                ChatRoomId = message.ChatRoomId,
                SenderUserId = message.SenderUserId,
                Content = message.Content,
                CreatedAt = message.CreatedAt
            });

            return message;
        }

        public async Task<IEnumerable<Message>> GetMessagesAsync(int chatRoomId, int pageNumber, int pageSize)
        {
            var messages = await _dbContext.Messages
                 .Where(m => m.ChatRoomId == chatRoomId)
                 .OrderBy(m => m.CreatedAt)
                 .Skip((pageNumber - 1) * pageSize)
                 .Take(pageSize)
                 .ToListAsync();

            var result = new List<Message>();
            foreach (var m in messages)
            {
                var decryptionRequest = new DecryptionRequest
                {
                    CipherText = m.Content
                };

                var decryptionResponse = await _encryptionClient.PostAsJsonAsync("api/encryption/decrypt", decryptionRequest);
                if (decryptionResponse.IsSuccessStatusCode)
                {
                    var decryptionResult = await decryptionResponse.Content.ReadFromJsonAsync<string>();
                    string decryptedContent = decryptionResult ?? "";
                    m.Content = decryptedContent;
                }
                else
                {
                    m.Content = "Помилка дешифрування";
                }
                result.Add(m);
            }
            return result;
        }
    }
}
