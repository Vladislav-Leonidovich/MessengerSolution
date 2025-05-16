using ChatService.Data;
using ChatService.Repositories;
using ChatService.Repositories.Interfaces;
using MessageServiceDTOs;
using Microsoft.EntityFrameworkCore;
using Shared.Exceptions;

namespace ChatService.Authorization
{
    public class ChatAuthorizationService : IChatAuthorizationService
    {
        private readonly IChatRoomRepository _chatRoomRepository;
        private readonly FolderRepository _folderRepository;
        private readonly ILogger<ChatAuthorizationService> _logger;

        public ChatAuthorizationService(IChatRoomRepository chatRoomRepository, FolderRepository folderRepository, ILogger<ChatAuthorizationService> logger)
        {
            _chatRoomRepository = chatRoomRepository;
            _folderRepository = folderRepository;
            _logger = logger;
        }

        public async Task<bool> CanAccessChatRoomAsync(int userId, int chatRoomId)
        {
            _logger.LogInformation("Перевірка доступу до чату {ChatRoomId} для користувача {UserId}", chatRoomId, userId);

            var chatRoomType = await _chatRoomRepository.GetChatRoomTypeByIdAsync(chatRoomId);

            switch(chatRoomType)
            {
                case ChatRoomType.privateChat:
                    return await _chatRoomRepository.CanAccessPrivateChatAsync(userId, chatRoomId);
                case ChatRoomType.groupChat:
                    return await _chatRoomRepository.CanAccessGroupChatAsync(userId, chatRoomId);
                default:
                    throw new ArgumentOutOfRangeException(nameof(chatRoomType), chatRoomType, null);
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

        public async Task<bool> CanAccessFolderAsync(int userId, int folderId)
        {
            return await _folderRepository.CanAccessFolderAsync(userId, folderId);
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
            var chatRoomType = await _chatRoomRepository.GetChatRoomTypeByIdAsync(chatRoomId);

            switch (chatRoomType)
            {
                case ChatRoomType.privateChat:
                    return await _chatRoomRepository.CanAccessPrivateChatAsync(userId, chatRoomId);
                case ChatRoomType.groupChat:
                    if (await _chatRoomRepository.CanAccessGroupChatAsync(userId, chatRoomId))
                    {
                        var ownerId = await _chatRoomRepository.GetOwnerGroupChatAsync(chatRoomId);
                        if (ownerId == userId)
                        {
                            return true;
                        }
                    }
                    return false;
                default:
                    throw new ArgumentOutOfRangeException(nameof(chatRoomType), chatRoomType, null);
            }
        }

        public async Task<bool> CanAddUserToChatAsync(int userId, int chatRoomId, int targetUserId)
        {
            var chatRoomType = await _chatRoomRepository.GetChatRoomTypeByIdAsync(chatRoomId);
            if (chatRoomType == ChatRoomType.privateChat)
            {
                return false; // Не можна додати користувача до приватного чату
            }

            // Перевіряємо, чи є користувач адміном
            var userRole = await _chatRoomRepository.GetUserRoleInGroupChatAsync(userId, chatRoomId);

            return userRole == ChatServiceModels.Chats.GroupRole.Admin;
        }
    }
}
