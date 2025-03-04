using MassTransit;
using MessageService.Data;
using MessageServiceDTOs;
using MessageService.Models;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts;
using EncryptionServiceDTOs;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

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
            if (!await IsAuthUserInChatRoomsAsync(model.ChatRoomId))
            {
                throw new UnauthorizedAccessException("Користувач не має доступу до цього чату.");
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
            var encryptionResult = await encryptionResponse.Content.ReadFromJsonAsync<DecryptionRequest>();
            if (encryptionResult == null)
            {
                throw new Exception("Не вдалося отримати результат шифрування.");
            }
            string encryptedContent = encryptionResult.CipherText ?? throw new Exception("Немає зашифрованого вмісту.");

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

            if (!await IsAuthUserInChatRoomsAsync(chatRoomId))
            {
                throw new UnauthorizedAccessException("Користувач не має доступу до цього чату.");
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
                    var decryptionResult = await decryptionResponse.Content.ReadFromJsonAsync<EncryptionRequest>();
                    if (decryptionResult == null)
                    {
                        throw new Exception("Не вдалося отримати результат дешифрування.");
                    }
                    string decryptedContent = decryptionResult.PlainText ?? "";
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

            if (!await IsAuthUserInChatRoomsAsync(chatRoomId))
            {
                throw new UnauthorizedAccessException("Користувач не має доступу до цього чату.");
            }

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

        public async Task<MessageDto> GetLastMessagePreviewByChatRoomIdAsync(int chatRoomId)
        {
            // Отримуємо поточний userId із токену
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
            {
                throw new UnauthorizedAccessException("Користувача не знайдено в токені.");
            }

            if (!await IsAuthUserInChatRoomsAsync(chatRoomId))
            {
                throw new UnauthorizedAccessException("Користувач не має доступу до цього чату.");
            }

            var lastMessage = await _dbContext.Messages
                .Where(m => m.ChatRoomId == chatRoomId)
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefaultAsync();

            if (lastMessage == null)
            {
                throw new Exception("Немає повідомлень.");
            }

            var decryptionRequest = new DecryptionRequest
            {
                CipherText = lastMessage.Content
            };

            var decryptionResponse = await _encryptionClient.PostAsJsonAsync("api/encryption/decrypt", decryptionRequest);
            string decryptedContent;
            if (!decryptionResponse.IsSuccessStatusCode)
            {
               decryptedContent = "Помилка дешифрування";
            }

            var decryptionResult = await decryptionResponse.Content.ReadFromJsonAsync<EncryptionRequest>();
            if (decryptionResult == null)
            {
                throw new Exception("Не вдалося отримати результат дешифрування.");
            }
            decryptedContent = decryptionResult.PlainText ?? "Немає повідомлень";

            var messageDto = new MessageDto
            {
                Id = lastMessage.Id,
                ChatRoomId = lastMessage.ChatRoomId,
                SenderUserId = lastMessage.SenderUserId,
                Content = decryptedContent,
                CreatedAt = lastMessage.CreatedAt
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
