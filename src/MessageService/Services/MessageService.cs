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
        private readonly IBus _bus;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly HttpClient _chatClient;
        private readonly HttpClient _encryptionClient;
        private readonly HttpClient _identityClient;

        public MessageService(MessageDbContext dbContext, IPublishEndpoint publishEndpoint, IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor, IBus bus)
        {
            _dbContext = dbContext;
            _bus = bus;
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
                ChatRoomType = model.ChatRoomType,
                SenderUserId = currentUserId,
                Content = encryptedContent,
                CreatedAt = DateTime.UtcNow
            };

            // Додаємо повідомлення в базу даних
            _dbContext.Messages.Add(message);
            await _dbContext.SaveChangesAsync();

            // Публікуємо подію MessageCreatedEvent
            var eventMessage = new MessageCreatedEvent
            {
                Id = message.Id,
                ChatRoomId = message.ChatRoomId,
                ChatRoomType = message.ChatRoomType,
                SenderUserId = message.SenderUserId,
                Content = message.Content,
                IsRead = message.IsRead,
                ReadAt = message.ReadAt,
                CreatedAt = message.CreatedAt,
                IsEdited = message.IsEdited,
                EditedAt = message.EditedAt
            };

            await _bus.Publish(eventMessage);

            var messageDto = new MessageDto
            {
                Id = message.Id,
                ChatRoomId = message.ChatRoomId,
                ChatRoomType = message.ChatRoomType,
                SenderUserId = message.SenderUserId,
                Content = message.Content,
                IsRead = message.IsRead,
                ReadAt = message.ReadAt,
                CreatedAt = message.CreatedAt,
                IsEdited = message.IsEdited,
                EditedAt = message.EditedAt
            };

            return messageDto;
        }

        public async Task<IEnumerable<MessageDto>> GetMessagesAsync(int chatRoomId, int startIndex, int count)
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
                 .Skip(startIndex)
                 .Take(count)
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
                    ChatRoomType = m.ChatRoomType,
                    SenderUserId = m.SenderUserId,
                    Content = m.Content,
                    CreatedAt = m.CreatedAt
                };

                result.Add(messageDto);
            }
            return result;
        }

        public async Task<ulong> GetMessagesCountByChatRoomIdAsync(int chatRoomId)
        {
            if (!await IsAuthUserInChatRoomsAsync(chatRoomId))
            {
                throw new UnauthorizedAccessException("Користувач не має доступу до цього чату.");
            }

            var totalCount = await _dbContext.Messages.Where(m => m.ChatRoomId == chatRoomId).CountAsync();

            return (ulong)totalCount;
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
                ChatRoomType = message.ChatRoomType,
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
                return new MessageDto();
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
                ChatRoomType = lastMessage.ChatRoomType,
                SenderUserId = lastMessage.SenderUserId,
                Content = decryptedContent,
                CreatedAt = lastMessage.CreatedAt
            };

            return messageDto;
        }

        public async Task<bool> DeleteMessageAsync(int messageId)
        {
            var message = await _dbContext.Messages.FindAsync(messageId);
            if (message == null)
            {
                return false;
            }
            var chatRoomId = message.ChatRoomId;

            if (!await IsAuthUserInChatRoomsAsync(chatRoomId))
            {
                throw new UnauthorizedAccessException("Користувач не має доступу до цього чату.");
            }
            _dbContext.Messages.Remove(message);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteMessagesByChatRoomIdAsync(int chatRoomId)
        {
            if (!await IsAuthUserInChatRoomsAsync(chatRoomId))
            {
                throw new UnauthorizedAccessException("Користувач не має доступу до цього чату.");
            }
            var messages = await _dbContext.Messages
                .Where(m => m.ChatRoomId == chatRoomId)
                .ToListAsync();

            _dbContext.Messages.RemoveRange(messages);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsAuthUserInChatRoomsAsync(int chatRoomId)
        {
            var response = await _chatClient.GetFromJsonAsync<bool>($"api/chat/get-auth-user-in-chat/{chatRoomId}");
            return response;
        }
    }
}
