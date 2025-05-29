using System.Linq;
using System.Security.Claims;
using ChatService.Data;
using ChatService.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System;
using MassTransit;
using Shared.Contracts;
using ChatService.Authorization;
using ChatService.Repositories.Interfaces;
using Shared.Exceptions;
using ChatService.Services.Interfaces;
using ChatService.Mappers.Interfaces;
using Shared.DTOs.Chat;
using Shared.DTOs.Responses;
using ChatService.Sagas.ChatCreation.Events;
using System.Diagnostics.Tracing;
using System.Text.Json;

namespace ChatService.Services
{
    public class ChatService : IChatService
    {
        private readonly IChatRoomRepository _chatRoomRepository;
        private readonly IChatAuthorizationService _authService;
        private readonly IChatOperationService _chatOperationService;
        private readonly IBus _bus;
        private readonly ILogger<ChatService> _logger;

        public ChatService(
            IChatRoomRepository chatRoomRepository,
            IChatAuthorizationService authService,
            IChatOperationService chatOperationService,
            IBus bus,
            ILogger<ChatService> logger)
        {
            _chatRoomRepository = chatRoomRepository;
            _authService = authService;
            _chatOperationService = chatOperationService;
            _bus = bus;
            _logger = logger;
        }

        public async Task<ApiResponse<dynamic>> GetChatByIdAsync(int chatRoomId, int userId)
        {
            try
            {
                // Перевірка доступу
                await _authService.EnsureCanAccessChatRoomAsync(userId, chatRoomId);

                // Отримання базової інформації про чат
                var chatRoom = await _chatRoomRepository.GetChatRoomTypeByIdAsync(chatRoomId);

                // Залежно від типу повертаємо відповідне DTO
                if (chatRoom == ChatRoomType.privateChat)
                {
                    var privateChat = await _chatRoomRepository.GetPrivateChatByIdAsync(chatRoomId);
                    if (privateChat == null)
                    {
                        throw new EntityNotFoundException("PrivateChat", chatRoomId);
                    }
                    return ApiResponse<dynamic>.Ok(privateChat);
                }
                else
                {
                    var groupChat = await _chatRoomRepository.GetGroupChatByIdAsync(chatRoomId);
                    if (groupChat == null)
                    {
                        throw new EntityNotFoundException("GroupChat", chatRoomId);
                    }
                    return ApiResponse<dynamic>.Ok(groupChat);
                }
            }
            catch (EntityNotFoundException ex)
            {
                _logger.LogWarning(ex.Message);
                return ApiResponse<dynamic>.Fail(ex.Message);
            }
            catch (ForbiddenAccessException ex)
            {
                _logger.LogWarning(ex.Message);
                return ApiResponse<dynamic>.Fail(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при отриманні чату {ChatId}", chatRoomId);
                return ApiResponse<dynamic>.Fail("Сталася внутрішня помилка сервера");
            }
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
                var correlationId = Guid.NewGuid();

                _logger.LogInformation("Створення приватного чату для користувача {UserId} з кореляційним ID {CorrelationId}", userId, correlationId);

                await _bus.Publish(new ChatCreationStartedEvent
                {
                    CorrelationId = correlationId,
                    ChatRoomId = 0, // Буде згенеровано в сазі
                    CreatorUserId = userId,

                });

                var operation = await _chatOperationService.WaitForOperationCompletionAsync(correlationId);

                if (operation.IsSuccessful)
                {
                    // Отримуємо створений чат за ID з результату операції
                    int chatRoomId = _chatOperationService.ExtractChatRoomIdFromResult(operation.Result);
                    var chat = await _chatRoomRepository.GetPrivateChatByIdAsync(chatRoomId);

                    if (chat == null)
                    {
                        return ApiResponse<ChatRoomDto>.Fail("Чат створено, але не вдалося отримати його дані");
                    }

                    return ApiResponse<ChatRoomDto>.Ok(chat, "Груповий чат успішно створено");
                }
                else
                {
                    // Якщо операція не вдалася, повертаємо помилку
                    return ApiResponse<ChatRoomDto>.Fail(operation.ErrorMessage ?? "Помилка при створенні чату");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при створенні приватного чату");
                throw;
            }
        }

        public async Task<ApiResponse<bool>> DeleteChatByIdAsync(int chatRoomId, int userId)
        {
            try
            {
                // Перевірка доступу
                await _authService.EnsureCanAccessChatRoomAsync(userId, chatRoomId);

                // Видалення чату
                var result = await _chatRoomRepository.DeleteChatAsync(chatRoomId);

                if (result)
                {
                    // Публікація події видалення чату
                    await _bus.Publish(new ChatDeletedEvent { ChatRoomId = chatRoomId });

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

        public async Task<ApiResponse<bool>> DeletePrivateChatByIdAsync(int chatRoomId, int userId)
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
                    await _bus.Publish(new ChatDeletedEvent { ChatRoomId = chatRoomId });

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
                var correlationId = Guid.NewGuid();

                _logger.LogInformation("Створення групового чату {ChatName} для користувача {UserId} з кореляційним ID {CorrelationId}",
                    dto.Name, userId, correlationId);

                await _bus.Publish(new ChatCreationStartedEvent
                {
                    CorrelationId = correlationId,
                    ChatRoomId = 0, // Буде згенеровано в сазі
                    CreatorUserId = userId,
                    MemberIds = dto.MemberIds,
                    ChatName = dto.Name
                });

                var operation = await _chatOperationService.WaitForOperationCompletionAsync(correlationId);

                if (operation.IsSuccessful)
                {
                    // Отримуємо створений чат за ID з результату операції
                    int chatRoomId = _chatOperationService.ExtractChatRoomIdFromResult(operation.Result);
                    var chat = await _chatRoomRepository.GetGroupChatByIdAsync(chatRoomId);

                    if (chat == null)
                    {
                        return ApiResponse<GroupChatRoomDto>.Fail("Чат створено, але не вдалося отримати його дані");
                    }

                    return ApiResponse<GroupChatRoomDto>.Ok(chat, "Груповий чат успішно створено");
                }
                else
                {
                    // Якщо операція не вдалася, повертаємо помилку
                    return ApiResponse<GroupChatRoomDto>.Fail(operation.ErrorMessage ?? "Помилка при створенні чату");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при створенні групового чату");
                throw;
            }
        }

        public async Task<ApiResponse<bool>> DeleteGroupChatByIdAsync(int chatRoomId, int userId)
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
                    await _bus.Publish(new ChatDeletedEvent { ChatRoomId = chatRoomId });
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
