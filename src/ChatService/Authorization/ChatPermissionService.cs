using ChatService.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Authorization;
using Shared.Authorization.Permissions;
using Shared.Exceptions;

namespace ChatService.Authorization
{
    public class ChatPermissionService : IPermissionService<ChatPermission>
    {
        private readonly ChatDbContext _context;
        private readonly ILogger<ChatPermissionService> _logger;

        public ChatPermissionService(ChatDbContext context, ILogger<ChatPermissionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> HasPermissionAsync(int userId, ChatPermission permission, int? resourceId = null)
        {
            _logger.LogInformation(
                "Перевірка дозволу {Permission} для користувача {UserId} та ресурсу {ResourceId}",
                permission, userId, resourceId);

            switch (permission)
            {
                case ChatPermission.ViewChat:
                    return await CanViewChatAsync(userId, resourceId);

                case ChatPermission.AddUserToChat:
                case ChatPermission.RemoveUserFromChat:
                case ChatPermission.ManageChatSettings:
                    return await IsOwnerOrAdminAsync(userId, resourceId);

                case ChatPermission.ViewFolder:
                case ChatPermission.UpdateFolder:
                case ChatPermission.DeleteFolder:
                    return await IsFolderOwnerAsync(userId, resourceId);

                case ChatPermission.CreateFolder:
                    // Будь-який авторизований користувач може створювати папки
                    return true;

                case ChatPermission.AssignChatToFolder:
                    return await CanAssignChatToFolderAsync(userId, resourceId);

                default:
                    _logger.LogWarning("Невідомий дозвіл {Permission}", permission);
                    return false;
            }
        }

        public async Task CheckPermissionAsync(int userId, ChatPermission permission, int? resourceId = null)
        {
            if (!await HasPermissionAsync(userId, permission, resourceId))
            {
                var resourceName = GetResourceName(permission);
                throw new ForbiddenAccessException(
                    $"У вас немає дозволу {permission} для {resourceName} {resourceId}");
            }
        }

        private async Task<bool> CanViewChatAsync(int userId, int? chatRoomId)
        {
            if (!chatRoomId.HasValue) return false;

            // Перевірка для приватного чату
            var isPrivateChatMember = await _context.UserChatRooms
                .AnyAsync(ucr => ucr.PrivateChatRoomId == chatRoomId && ucr.UserId == userId);

            if (isPrivateChatMember) return true;

            // Перевірка для групового чату
            var isGroupChatMember = await _context.GroupChatMembers
                .AnyAsync(gcm => gcm.GroupChatRoomId == chatRoomId && gcm.UserId == userId);

            return isGroupChatMember;
        }

        private async Task<bool> IsOwnerOrAdminAsync(int userId, int? chatRoomId)
        {
            if (!chatRoomId.HasValue) return false;

            // Перевіряємо, чи є користувач власником
            var isOwner = await _context.GroupChatRooms
                .AnyAsync(gcr => gcr.Id == chatRoomId && gcr.OwnerId == userId);

            if (isOwner) return true;

            // Перевіряємо, чи є адміністратором
            var isAdmin = await _context.GroupChatMembers
                .AnyAsync(gcm =>
                    gcm.GroupChatRoomId == chatRoomId &&
                    gcm.UserId == userId &&
                    gcm.Role == ChatServiceModels.Chats.GroupRole.Admin);

            return isAdmin;
        }

        private async Task<bool> IsFolderOwnerAsync(int userId, int? folderId)
        {
            if (!folderId.HasValue) return false;

            return await _context.Folders
                .AnyAsync(f => f.Id == folderId.Value && f.UserId == userId);
        }

        private async Task<bool> CanAssignChatToFolderAsync(int userId, int? chatRoomId)
        {
            // Користувач повинен мати доступ до чату та бути власником папки
            if (await IsFolderOwnerAsync(userId, chatRoomId))
            {
                return await CanViewChatAsync(userId, chatRoomId);
            }

            return false;
        }

        private string GetResourceName(ChatPermission permission)
        {
            return permission.ToString().Contains("Folder") ? "папки" : "чату";
        }
    }
}
