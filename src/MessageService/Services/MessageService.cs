using MassTransit;
using MessageService.Data;
using MessageServiceDTOs;
using MessageService.Models;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts;
using EncryptionServiceDTOs;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace MessageService.Services
{
    public class MessageService : IMessageService
    {
        private readonly MessageDbContext _dbContext;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly HttpClient _chatClient;
        private readonly HttpClient _encryptionClient;
        private readonly HttpClient _identityClient;

        public MessageService(MessageDbContext dbContext, IPublishEndpoint publishEndpoint, IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _dbContext = dbContext;
            _publishEndpoint = publishEndpoint;
            _chatClient = httpClientFactory.CreateClient("ChatClient");
            _encryptionClient = httpClientFactory.CreateClient("EncryptionClient");
            _identityClient = httpClientFactory.CreateClient("IdentityClient");
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<MessageDto> SendMessageAsync(SendMessageDto model)
        {
            // Отримуємо поточний userId із токену
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
            {
                throw new UnauthorizedAccessException("Користувача не знайдено в токені.");
            }

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
                SenderUserId = currentUserId,
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

            var messageDto = new MessageDto
            {
                Id = message.Id,
                ChatRoomId = message.ChatRoomId,
                SenderUserId = message.SenderUserId,
                Content = message.Content,
                CreatedAt = message.CreatedAt
            };

            return messageDto;
        }

        public async Task<IEnumerable<MessageDto>> GetMessagesAsync(int chatRoomId, int pageNumber, int pageSize)
        {
            // Отримуємо поточний userId із токену
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
            {
                throw new UnauthorizedAccessException("Користувача не знайдено в токені.");
            }

            var messages = await _dbContext.Messages
                 .Where(m => m.ChatRoomId == chatRoomId)
                 .OrderBy(m => m.CreatedAt)
                 .Skip((pageNumber - 1) * pageSize)
                 .Take(pageSize)
                 .ToListAsync();

            var result = new List<MessageDto>();
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

                var messageDto = new MessageDto
                {
                    Id = m.Id,
                    ChatRoomId = m.ChatRoomId,
                    SenderUserId = m.SenderUserId,
                    Content = m.Content,
                    CreatedAt = m.CreatedAt
                };

                result.Add(messageDto);
            }
            return result;
        }

        public async Task<MessageDto> MarkMessageAsRead(int messageId)
        {
            // Отримуємо поточний userId із токену
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
            {
                throw new UnauthorizedAccessException("Користувача не знайдено в токені.");
            }

            var message = await _dbContext.Messages.FindAsync(messageId);
            if (message == null)
            {
                return new MessageDto();
            }

            var chatRoomId = message.ChatRoomId;
            await IsAuthUserInChatRoomsAsync(chatRoomId);

            message.IsRead = true;
            message.ReadAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            var messageDto = new MessageDto
            {
                Id = message.Id,
                ChatRoomId = message.ChatRoomId,
                SenderUserId = message.SenderUserId,
                Content = message.Content,
                CreatedAt = message.CreatedAt
            };

            return messageDto;
        }

        private async Task<bool> IsAuthUserInChatRoomsAsync(int chatRoomId)
        {
            var response = await _chatClient.GetFromJsonAsync<bool>($"api/chat/get-auth-user-in-chat/{chatRoomId}");
            return response;
        }
    }
}
