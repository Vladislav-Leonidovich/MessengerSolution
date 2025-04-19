using System.Linq;
using System.Security.Claims;
using ChatService.Data;
using ChatServiceDTOs.Chats;
using ChatServiceModels.Chats;
using ChatService.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using Shared.IdentityServiceDTOs;
using MessageServiceDTOs;
using System;
using MassTransit;
using Shared.Contracts;
using ChatService.Authorization;
using ChatService.Repositories.Interfaces;
using Shared.Exceptions;
using Shared.Responses;
using ChatService.Services.Interfaces;

namespace ChatService.Services
{
    public class ChatService : IChatService
    {
        private readonly IChatRoomRepository _chatRoomRepository;
        private readonly IChatAuthorizationService _authService;
        private readonly IBus _bus;
        private readonly ILogger<ChatService> _logger;

        public ChatService(
            IChatRoomRepository chatRoomRepository,
            IChatAuthorizationService authService,
            IBus bus,
            ILogger<ChatService> logger)
        {
            _chatRoomRepository = chatRoomRepository;
            _authService = authService;
            _bus = bus;
            _logger = logger;
        }

        public async Task<ApiResponse<ChatRoomDto>> GetPrivateChatByIdAsync(int chatRoomId, int userId)
        {
            try
            {
                // Перевірка доступу
                await _authService.EnsureCanAccessChatRoomAsync(userId, chatRoomId);

                var chat = await _chatRoomRepository.GetPrivateChatByIdAsync(chatRoomId);
                if (chat == null)
                {
                    throw new EntityNotFoundException("PrivateChat", chatRoomId);
                }

                return ApiResponse<ChatRoomDto>.Ok(chat);
            }
            catch (EntityNotFoundException ex)
            {
                _logger.LogWarning(ex.Message);
                return ApiResponse<ChatRoomDto>.Fail(ex.Message);
            }
            catch (ForbiddenAccessException ex)
            {
                _logger.LogWarning(ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при отриманні приватного чату {ChatId}", chatRoomId);
                throw;
            }
        }

        public async Task<ApiResponse<IEnumerable<ChatRoomDto>>> GetPrivateChatsForUserAsync(int userId)
        {
            try
            {
                var chats = await _chatRoomRepository.GetPrivateChatsForUserAsync(userId);
                return ApiResponse<IEnumerable<ChatRoomDto>>.Ok(chats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при отриманні приватних чатів для користувача {UserId}", userId);
                throw;
            }
        }

        public async Task<ApiResponse<ChatRoomDto>> CreatePrivateChatAsync(CreatePrivateChatRoomDto dto, int userId)
        {
            try
            {
                var chat = await _chatRoomRepository.CreatePrivateChatAsync(dto, userId);
                return ApiResponse<ChatRoomDto>.Ok(chat, "Приватний чат успішно створено");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при створенні приватного чату");
                throw;
            }
        }

        public async Task<ApiResponse<bool>> DeletePrivateChatAsync(int chatRoomId, int userId)
        {
            try
            {
                // Перевірка доступу
                await _authService.EnsureCanAccessChatRoomAsync(userId, chatRoomId);

                // Видалення чату
                var result = await _chatRoomRepository.DeletePrivateChatAsync(chatRoomId);

                if (result)
                {
                    // Публікація події видалення чату
                    await _bus.Publish(new ChatEvents { ChatRoomId = chatRoomId });

                    return ApiResponse<bool>.Ok(true, "Чат видалено успішно");
                }

                return ApiResponse<bool>.Fail("Не вдалося видалити чат");
            }
            catch (EntityNotFoundException ex)
            {
                _logger.LogWarning(ex.Message);
                return ApiResponse<bool>.Fail(ex.Message);
            }
            catch (ForbiddenAccessException ex)
            {
                _logger.LogWarning(ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при видаленні приватного чату {ChatId}", chatRoomId);
                throw;
            }
        }

        public async Task<bool> IsUserInChatAsync(int userId, int chatRoomId)
        {
            try
            {
                return await _chatRoomRepository.UserBelongsToChatAsync(userId, chatRoomId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при перевірці участі користувача {UserId} в чаті {ChatId}",
                    userId, chatRoomId);
                throw;
            }
        }

        public async Task<ApiResponse<GroupChatRoomDto>> GetGroupChatByIdAsync(int chatRoomId, int userId)
        {
            try
            {
                // Перевірка доступу
                await _authService.EnsureCanAccessChatRoomAsync(userId, chatRoomId);
                var chat = await _chatRoomRepository.GetGroupChatByIdAsync(chatRoomId);
                if (chat == null)
                {
                    throw new EntityNotFoundException("GroupChat", chatRoomId);
                }
                return ApiResponse<GroupChatRoomDto>.Ok(chat);
            }
            catch (EntityNotFoundException ex)
            {
                _logger.LogWarning(ex.Message);
                return ApiResponse<GroupChatRoomDto>.Fail(ex.Message);
            }
            catch (ForbiddenAccessException ex)
            {
                _logger.LogWarning(ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при отриманні групового чату {ChatId}", chatRoomId);
                throw;
            }
        }

        public async Task<ApiResponse<IEnumerable<GroupChatRoomDto>>> GetGroupChatsForUserAsync(int userId)
        {
            try
            {
                var chats = await _chatRoomRepository.GetGroupChatsForUserAsync(userId);
                return ApiResponse<IEnumerable<GroupChatRoomDto>>.Ok(chats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при отриманні групових чатів для користувача {UserId}", userId);
                throw;
            }
        }

        public async Task<ApiResponse<IEnumerable<ChatRoomDto>>> GetPrivateChatsForFolderAsync(int folderId, int userId)
        {
            try
            {
                var chats = await _chatRoomRepository.GetPrivateChatsForFolderAsync(folderId);
                return ApiResponse<IEnumerable<ChatRoomDto>>.Ok(chats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при отриманні приватних чатів для папки {FolderId}", folderId);
                throw;
            }
        }

        public async Task<ApiResponse<IEnumerable<GroupChatRoomDto>>> GetGroupChatsForFolderAsync(int folderId, int userId)
        {
            try
            {
                var chats = await _chatRoomRepository.GetGroupChatsForFolderAsync(folderId);
                return ApiResponse<IEnumerable<GroupChatRoomDto>>.Ok(chats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при отриманні групових чатів для папки {FolderId}", folderId);
                throw;
            }
        }

        public async Task<ApiResponse<IEnumerable<ChatRoomDto>>> GetPrivateChatsWithoutFolderAsync(int userId)
        {
            try
            {
                var chats = await _chatRoomRepository.GetPrivateChatsWithoutFolderAsync(userId);
                return ApiResponse<IEnumerable<ChatRoomDto>>.Ok(chats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при отриманні приватних чатів без папки для користувача {UserId}", userId);
                throw;
            }
        }

        public async Task<ApiResponse<IEnumerable<GroupChatRoomDto>>> GetGroupChatsWithoutFolderAsync(int userId)
        {
            try
            {
                var chats = await _chatRoomRepository.GetGroupChatsWithoutFolderAsync(userId);
                return ApiResponse<IEnumerable<GroupChatRoomDto>>.Ok(chats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при отриманні групових чатів без папки для користувача {UserId}", userId);
                throw;
            }
        }

        public async Task<ApiResponse<GroupChatRoomDto>> CreateGroupChatAsync(CreateGroupChatRoomDto dto, int userId)
        {
            try
            {
                var chat = await _chatRoomRepository.CreateGroupChatAsync(dto, userId);
                return ApiResponse<GroupChatRoomDto>.Ok(chat, "Груповий чат успішно створено");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при створенні групового чату");
                throw;
            }
        }

        public async Task<ApiResponse<bool>> DeleteGroupChatAsync(int chatRoomId, int userId)
        {
            try
            {
                // Перевірка доступу
                await _authService.EnsureCanAccessChatRoomAsync(userId, chatRoomId);
                // Видалення чату
                var result = await _chatRoomRepository.DeleteGroupChatAsync(chatRoomId);
                if (result)
                {
                    // Публікація події видалення чату
                    await _bus.Publish(new ChatEvents { ChatRoomId = chatRoomId });
                    return ApiResponse<bool>.Ok(true, "Чат видалено успішно");
                }
                return ApiResponse<bool>.Fail("Не вдалося видалити чат");
            }
            catch (EntityNotFoundException ex)
            {
                _logger.LogWarning(ex.Message);
                return ApiResponse<bool>.Fail(ex.Message);
            }
            catch (ForbiddenAccessException ex)
            {
                _logger.LogWarning(ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при видаленні групового чату {ChatId}", chatRoomId);
                throw;
            }
        }
    }
}
