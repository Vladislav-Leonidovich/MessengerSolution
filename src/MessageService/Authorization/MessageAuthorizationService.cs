using MessageService.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Exceptions;

namespace MessageService.Authorization
{
    public class MessageAuthorizationService : IMessageAuthorizationService
    {
        private readonly MessageDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<MessageAuthorizationService> _logger;

        public MessageAuthorizationService(
            MessageDbContext context,
            IHttpClientFactory httpClientFactory,
            ILogger<MessageAuthorizationService> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<bool> CanAccessMessageAsync(int userId, int messageId)
        {
            _logger.LogInformation("Перевірка доступу до повідомлення {MessageId} для користувача {UserId}",
                messageId, userId);

            var message = await _context.Messages
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (message == null)
            {
                return false;
            }

            if (message.SenderUserId == userId)
            {
                return true;
            }

            return await CanAccessChatRoomAsync(userId, message.ChatRoomId);
        }

        public async Task<bool> CanAccessChatRoomAsync(int userId, int chatRoomId)
        {
            try
            {
                var chatClient = _httpClientFactory.CreateClient("ChatClient");
                var response = await chatClient.GetAsync($"api/chat/get-auth-user-in-chat/{chatRoomId}");

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка під час перевірки доступу до чату {ChatRoomId}", chatRoomId);
                return false;
            }
        }

        public async Task EnsureCanAccessMessageAsync(int userId, int messageId)
        {
            if (!await CanAccessMessageAsync(userId, messageId))
            {
                _logger.LogWarning("Відмовлено в доступі до повідомлення {MessageId} для користувача {UserId}",
                    messageId, userId);

                throw new ForbiddenAccessException($"У вас немає доступу до повідомлення з ID {messageId}");
            }
        }

        public async Task EnsureCanAccessChatRoomAsync(int userId, int chatRoomId)
        {
            if (!await CanAccessChatRoomAsync(userId, chatRoomId))
            {
                _logger.LogWarning("Відмовлено в доступі до чату {ChatRoomId} для користувача {UserId}",
                    chatRoomId, userId);

                throw new ForbiddenAccessException($"У вас немає доступу до чату з ID {chatRoomId}");
            }
        }
    }
}
