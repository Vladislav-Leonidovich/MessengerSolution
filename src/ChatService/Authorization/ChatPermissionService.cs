using ChatService.Data;
using ChatService.Repositories.Interfaces;
using ChatServiceModels.Chats;
using MessageServiceDTOs;
using Microsoft.EntityFrameworkCore;
using Shared.Authorization;
using Shared.Authorization.Permissions;
using Shared.Exceptions;

namespace ChatService.Authorization
{
    public class ChatPermissionService : IPermissionService<ChatPermission>
    {
        private readonly IChatRoomRepository _chatRoomRepository;
        private readonly IFolderRepository _folderRepository;
        private readonly ILogger<ChatPermissionService> _logger;

        public ChatPermissionService(IChatRoomRepository chatRoomRepository, IFolderRepository folderRepository, ILogger<ChatPermissionService> logger)
        {
            _chatRoomRepository = chatRoomRepository;
            _folderRepository = folderRepository;
            _logger = logger;
        }

        public async Task<bool> HasPermissionAsync(int userId, ChatPermission permission, int? resourceId = null)
        {
            _logger.LogInformation(
                "Перевірка дозволу {Permission} для користувача {UserId} та ресурсу {ResourceId}",
                permission, userId, resourceId);

            switch (permission)
            {
                case ChatPermission.CreateChat:
                    return true; // Будь-який авторизований користувач може створювати чати

                case ChatPermission.ViewChat:
                    return await CanViewChatAsync(userId, resourceId);

                case ChatPermission.DeleteChat:
                    return await CanDeleteChatAsync(userId, resourceId);

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

                case ChatPermission.UnassignChatToFolder:
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

        private async Task<bool> CanDeleteChatAsync(int userId, int? chatRoomId)
        {
            if (!chatRoomId.HasValue) return false;

            var chatRoomType = await _chatRoomRepository.GetChatRoomTypeByIdAsync(chatRoomId.Value);

            switch (chatRoomType)
            {
                case ChatRoomType.privateChat:
                    return await _chatRoomRepository.UserBelongsToChatAsync(userId, chatRoomId.Value);
                case ChatRoomType.groupChat:
                    return await IsOwnerOrAdminAsync(userId, chatRoomId);
                default:
                    return false;
            }
        }

        private async Task<bool> CanViewChatAsync(int userId, int? chatRoomId)
        {
            if (!chatRoomId.HasValue) return false;

            var chatRoomType = await _chatRoomRepository.GetChatRoomTypeByIdAsync(chatRoomId.Value);

            switch(chatRoomType)
            {
                case ChatRoomType.privateChat:
                    return await _chatRoomRepository.CanAccessPrivateChatAsync(userId, chatRoomId.Value);
                case ChatRoomType.groupChat:
                    return await _chatRoomRepository.CanAccessGroupChatAsync(userId, chatRoomId.Value);
                default:
                    return false;
            }
        }

        private async Task<bool> IsOwnerOrAdminAsync(int userId, int? chatRoomId)
        {
            if (!chatRoomId.HasValue) return false;

            // Перевіряємо, чи є користувач власником
            var isOwner = await _chatRoomRepository
                .GetOwnerGroupChatAsync(chatRoomId.Value) == userId;

            if (isOwner) return true;

            // Перевіряємо, чи є адміністратором
            var isAdmin = await _chatRoomRepository
                .GetUserRoleInGroupChatAsync(userId, chatRoomId.Value) == GroupRole.Admin;

            return isAdmin;
        }

        private async Task<bool> IsFolderOwnerAsync(int userId, int? folderId)
        {
            if (!folderId.HasValue) return false;

            return await _folderRepository.IsFolderOwnerAsync(userId, folderId.Value);
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
