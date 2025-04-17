using ChatService.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Exceptions;

namespace ChatService.Authorization
{
    public class ChatAuthorizationService : IChatAuthorizationService
    {
        private readonly ChatDbContext _context;
        private readonly ILogger<ChatAuthorizationService> _logger;

        public ChatAuthorizationService(ChatDbContext context, ILogger<ChatAuthorizationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> CanAccessChatRoomAsync(int userId, int chatRoomId)
        {
            _logger.LogInformation("Перевірка доступу до чату {ChatRoomId} для користувача {UserId}", chatRoomId, userId);

            // Перевіряємо доступ до приватного чату
            var isPrivateChatMember = await _context.UserChatRooms
                .AnyAsync(ucr => ucr.PrivateChatRoomId == chatRoomId && ucr.UserId == userId);

            if (isPrivateChatMember) return true;

            // Перевіряємо доступ до групового чату
            var isGroupChatMember = await _context.GroupChatMembers
                .AnyAsync(gcm => gcm.GroupChatRoomId == chatRoomId && gcm.UserId == userId);

            return isGroupChatMember;
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

        public async Task<bool> CanAccessFolderAsync(int userId, int folderId)
        {
            return await _context.Folders
                .AnyAsync(f => f.Id == folderId && f.UserId == userId);
        }

        public async Task EnsureCanAccessFolderAsync(int userId, int folderId)
        {
            if (!await CanAccessFolderAsync(userId, folderId))
            {
                _logger.LogWarning("Відмовлено в доступі до папки {FolderId} для користувача {UserId}",
                    folderId, userId);

                throw new ForbiddenAccessException($"У вас немає доступу до папки з ID {folderId}");
            }
        }

        public async Task<bool> CanModifyChatAsync(int userId, int chatRoomId)
        {
            // Перевіряємо, чи є користувач власником групового чату
            var groupChat = await _context.GroupChatRooms
                .FirstOrDefaultAsync(g => g.Id == chatRoomId);

            if (groupChat != null)
            {
                return groupChat.OwnerId == userId;
            }

            // Для приватних чатів - перевіряємо участь
            return await CanAccessChatRoomAsync(userId, chatRoomId);
        }

        public async Task<bool> CanAddUserToChatAsync(int userId, int chatRoomId, int targetUserId)
        {
            // Тільки власник або адмін може додавати користувачів
            var groupChat = await _context.GroupChatRooms
                .FirstOrDefaultAsync(g => g.Id == chatRoomId);

            if (groupChat == null) return false; // Це не груповий чат

            if (groupChat.OwnerId == userId) return true; // Власник може все

            // Перевіряємо, чи є користувач адміном
            var userRole = await _context.GroupChatMembers
                .Where(m => m.GroupChatRoomId == chatRoomId && m.UserId == userId)
                .Select(m => m.Role)
                .FirstOrDefaultAsync();

            return userRole == ChatServiceModels.Chats.GroupRole.Admin;
        }
    }
}
