using MassTransit;
using Shared.Contracts;
using ChatService.Authorization;
using ChatService.Repositories.Interfaces;
using Shared.Exceptions;
using ChatService.Services.Interfaces;
using Shared.DTOs.Chat;
using Shared.DTOs.Responses;
using ChatService.Sagas.ChatCreation.Events;
using ChatService.Sagas.ChatOperation.Events;
using Microsoft.AspNetCore.SignalR;
using ChatService.Hubs;
using Shared.DTOs.Message;
using System;

namespace ChatService.Services
{
    public class ChatService : IChatService
    {
        private readonly IChatRoomRepository _chatRoomRepository;
        private readonly IChatAuthorizationService _authService;
        private readonly IChatOperationService _chatOperationService;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly IBus _bus;
        private readonly ILogger<ChatService> _logger;

        public ChatService(
            IChatRoomRepository chatRoomRepository,
            IChatAuthorizationService authService,
            IChatOperationService chatOperationService,
            IHubContext<ChatHub> _hubContext,
            IBus bus,
            ILogger<ChatService> logger)
        {
            _chatRoomRepository = chatRoomRepository;
            _authService = authService;
            _chatOperationService = chatOperationService;
            _bus = bus;
            _logger = logger;
        }

        // Методи для всіх чатів

        public async Task<ApiResponse<object>> GetChatByIdAsync(int chatRoomId, int userId)
        {
            try
            {
                // Перевірка доступу
                await _authService.EnsureCanAccessChatRoomAsync(userId, chatRoomId);

                // Отримання базової інформації про чат
                var chatRoomType = await _chatRoomRepository.GetChatRoomTypeByIdAsync(chatRoomId);

                // Залежно від типу повертаємо відповідне DTO
                if (chatRoomType == ChatRoomType.privateChat)
                {
                    var privateChat = await _chatRoomRepository.GetPrivateChatByIdAsync(chatRoomId);
                    if (privateChat == null)
                    {
                        throw new EntityNotFoundException("PrivateChat", chatRoomId);
                    }

                    return ApiResponse<object>.Ok(privateChat);
                }
                else
                {
                    var groupChat = await _chatRoomRepository.GetGroupChatByIdAsync(chatRoomId);
                    if (groupChat == null)
                    {
                        throw new EntityNotFoundException("GroupChat", chatRoomId);
                    }

                    var lastMessagePreview = await _chatRoomRepository.GetLastMessagePreviewAsync(chatRoomId);
                    groupChat.LastMessagePreview = lastMessagePreview;
                    return ApiResponse<object>.Ok(groupChat);
                }
            }
            catch (EntityNotFoundException ex)
            {
                _logger.LogWarning("Користувач {UserId} запросив неіснуючий чат {ChatRoomId}", userId, chatRoomId);
                return ApiResponse<object>.Fail(ex.Message);
            }
            catch (ForbiddenAccessException ex)
            {
                _logger.LogWarning("Відмовлено в доступі до чату {ChatRoomId} користувачу {UserId}", chatRoomId, userId);
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
                    //await _bus.Publish(new ChatDeletedEvent { ChatRoomId = chatRoomId });

                    return ApiResponse<bool>.Ok(true, "Чат видалено успішно");
                }

                return ApiResponse<bool>.Fail("Не вдалося видалити чат");
            }
            catch (EntityNotFoundException ex)
            {
                _logger.LogWarning("Користувач {UserId} не зміг видалити чат, бо чату {ChatRoomId} не існує", userId, chatRoomId);
                return ApiResponse<bool>.Fail(ex.Message);
            }
            catch (ForbiddenAccessException)
            {
                _logger.LogWarning("Відмовлено в доступі видалення чату {ChatRoomId} користувачу {UserId}", chatRoomId, userId);
                throw;
            }
        }

        // Методи для приватних чатів GET

        public async Task<ApiResponse<ChatRoomDto>> GetPrivateChatByIdAsync(int chatRoomId, int userId)
        {
            try
            {
                // Перевірка доступу
                await _authService.EnsureCanAccessChatRoomAsync(userId, chatRoomId);

                var chat = await _chatRoomRepository.GetPrivateChatByIdAsync(chatRoomId);
                if(chat == null)
                {
                    throw new EntityNotFoundException("PrivateChatRoom", chatRoomId);
                }

                return ApiResponse<ChatRoomDto>.Ok(chat);
            }
            catch (EntityNotFoundException ex)
            {
                _logger.LogWarning("Користувач {UserId} запросив неіснуючий приватний чат {ChatRoomId}", userId, chatRoomId);
                return ApiResponse<ChatRoomDto>.Fail(ex.Message);
            }
            catch (ForbiddenAccessException)
            {
                _logger.LogWarning("Відмовлено в доступі до приватного чату {ChatRoomId} користувачу {UserId}", chatRoomId, userId);
                throw;
            }
        }

        public async Task<ApiResponse<IEnumerable<ChatRoomDto>>> GetPrivateChatsForUserAsync(int userId)
        {
            try
            {
                var chats = await _chatRoomRepository.GetPrivateChatsForUserAsync(userId);
                var chatIds = chats.Select(chat => chat.Id).ToList();
                var dictionary = await _chatRoomRepository.GetLastMessagePreviewBatchAsync(chatIds);

                foreach (var chat in chats)
                {
                    if (dictionary.TryGetValue(chat.Id, out var lastMessage))
                    {
                        chat.LastMessagePreview = lastMessage;
                    }
                    chat.Name = await _chatRoomRepository.GetChatNameAsync(chat.Id, userId);
                }
                return ApiResponse<IEnumerable<ChatRoomDto>>.Ok(chats);
            }
            catch (EntityNotFoundException ex)
            {
                _logger.LogWarning("Користувач {UserId} запросив неіснуючі приватні чати", userId);
                return ApiResponse<IEnumerable<ChatRoomDto>>.Fail(ex.Message);
            }
            catch (ForbiddenAccessException)
            {
                _logger.LogWarning("Відмовлено в доступі до приватних чатів користувачу {UserId}", userId);
                throw;
            }
        }

        public async Task<ApiResponse<IEnumerable<ChatRoomDto>>> GetPrivateChatsForFolderAsync(int folderId, int userId)
        {
            try
            {
                var chats = await _chatRoomRepository.GetPrivateChatsForFolderAsync(folderId);
                var chatIds = chats.Select(chat => chat.Id).ToList();
                var dictionary = await _chatRoomRepository.GetLastMessagePreviewBatchAsync(chatIds);

                foreach (var chat in chats)
                {
                    if (dictionary.TryGetValue(chat.Id, out var lastMessage))
                    {
                        chat.LastMessagePreview = lastMessage;
                    }
                    chat.Name = await _chatRoomRepository.GetChatNameAsync(chat.Id, userId);
                }

                return ApiResponse<IEnumerable<ChatRoomDto>>.Ok(chats);
            }
            catch (EntityNotFoundException ex)
            {
                _logger.LogWarning("Користувач {UserId} запросив неіснуючі приватні чати, або ж папки {FolderId} не існує", userId, folderId);
                return ApiResponse<IEnumerable<ChatRoomDto>>.Fail(ex.Message);
            }
            catch (ForbiddenAccessException)
            {
                _logger.LogWarning("Відмовлено в доступі до приватних чатів з папки {FolderId}, користувачу {UserId}", folderId, userId);
                throw;
            }
        }

        public async Task<ApiResponse<IEnumerable<ChatRoomDto>>> GetPrivateChatsWithoutFolderAsync(int userId)
        {
            try
            {
                var chats = await _chatRoomRepository.GetPrivateChatsWithoutFolderAsync(userId);
                var chatIds = chats.Select(chat => chat.Id).ToList();
                var dictionary = await _chatRoomRepository.GetLastMessagePreviewBatchAsync(chatIds);

                foreach (var chat in chats)
                {
                    if (dictionary.TryGetValue(chat.Id, out var lastMessage))
                    {
                        chat.LastMessagePreview = lastMessage;
                    }
                    chat.Name = await _chatRoomRepository.GetChatNameAsync(chat.Id, userId);
                }
                return ApiResponse<IEnumerable<ChatRoomDto>>.Ok(chats);
            }
            catch (EntityNotFoundException ex)
            {
                _logger.LogWarning("Користувач {UserId} запросив неіснуючі приватні чати без папки", userId);
                return ApiResponse<IEnumerable<ChatRoomDto>>.Fail(ex.Message);
            }
            catch (ForbiddenAccessException)
            {
                _logger.LogWarning("Відмовлено в доступі до приватних чатів без папки, користувачу {UserId}", userId);
                throw;
            }
        }

        // Методи для приватних чатів CREATE

        public async Task<ApiResponse<ChatRoomDto>> CreatePrivateChatAsync(CreatePrivateChatRoomDto dto, int userId)
        {
            try
            {
                var chat = await _chatRoomRepository.CreatePrivateChatAsync(dto, userId);
                string groupName = chat.Id.ToString();
                //await _hubContext.Clients.Group(groupName).SendAsync("ChatCreated");

                return ApiResponse<ChatRoomDto>.Ok(chat, "Приватний чат успішно створено");
            }
            catch (EntityNotFoundException ex)
            {
                _logger.LogWarning("Користувач {UserId} не зміг створити приватний чат", userId);
                return ApiResponse<ChatRoomDto>.Fail(ex.Message);
            }
            catch (ForbiddenAccessException)
            {
                _logger.LogWarning("Відмовлено в доступі створення приватного чату користувачу {UserId}", userId);
                throw;
            }
        }

        // Методи для приватних чатів DELETE

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
                    string groupName = chatRoomId.ToString();
                    //await _hubContext.Clients.Group(groupName).SendAsync("ChatDeleted");

                    return ApiResponse<bool>.Ok(true, "Чат видалено успішно");
                }

                return ApiResponse<bool>.Fail("Не вдалося видалити приватний чат");
            }
            catch (EntityNotFoundException ex)
            {
                _logger.LogWarning("Користувач {UserId} не зміг видалити приватний чат, бо чату {ChatRoomId} не існує", userId, chatRoomId);
                return ApiResponse<bool>.Fail(ex.Message);
            }
            catch (ForbiddenAccessException)
            {
                _logger.LogWarning("Відмовлено в доступі видалення приватного чату {ChatRoomId} користувачу {UserId}", chatRoomId, userId);
                throw;
            }
        }

        // Методи для групових чатів GET

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
                _logger.LogWarning("Користувач {UserId} запросив неіснуючий груповий чат {ChatRoomId}", userId, chatRoomId);
                return ApiResponse<GroupChatRoomDto>.Fail(ex.Message);
            }
            catch (ForbiddenAccessException)
            {
                _logger.LogWarning("Відмовлено в доступі до групового чату {ChatRoomId} користувачу {UserId}", chatRoomId, userId);
                throw;
            }
        }

        public async Task<ApiResponse<IEnumerable<GroupChatRoomDto>>> GetGroupChatsForUserAsync(int userId)
        {
            try
            {
                var chats = await _chatRoomRepository.GetGroupChatsForUserAsync(userId);
                var chatIds = chats.Select(chat => chat.Id).ToList();
                var dictionary = await _chatRoomRepository.GetLastMessagePreviewBatchAsync(chatIds);

                foreach (var chat in chats)
                {
                    if (dictionary.TryGetValue(chat.Id, out var lastMessage))
                    {
                        chat.LastMessagePreview = lastMessage;
                    }
                }
                return ApiResponse<IEnumerable<GroupChatRoomDto>>.Ok(chats);
            }
            catch (EntityNotFoundException ex)
            {
                _logger.LogWarning("Користувач {UserId} запросив неіснуючі групові чати", userId);
                return ApiResponse<IEnumerable<GroupChatRoomDto>>.Fail(ex.Message);
            }
            catch (ForbiddenAccessException)
            {
                _logger.LogWarning("Відмовлено в доступі до групових чатів користувачу {UserId}", userId);
                throw;
            }
        }

        public async Task<ApiResponse<IEnumerable<GroupChatRoomDto>>> GetGroupChatsForFolderAsync(int folderId, int userId)
        {
            try
            {
                var chats = await _chatRoomRepository.GetGroupChatsForFolderAsync(folderId);
                var chatIds = chats.Select(chat => chat.Id).ToList();
                var dictionary = await _chatRoomRepository.GetLastMessagePreviewBatchAsync(chatIds);

                foreach (var chat in chats)
                {
                    if (dictionary.TryGetValue(chat.Id, out var lastMessage))
                    {
                        chat.LastMessagePreview = lastMessage;
                    }
                }
                return ApiResponse<IEnumerable<GroupChatRoomDto>>.Ok(chats);
            }
            catch (EntityNotFoundException ex)
            {
                _logger.LogWarning("Користувач {UserId} запросив неіснуючі групові чати, або ж папки {FolderId} не існує", userId, folderId);
                return ApiResponse<IEnumerable<GroupChatRoomDto>>.Fail(ex.Message);
            }
            catch (ForbiddenAccessException)
            {
                _logger.LogWarning("Відмовлено в доступі до групових чатів з папки {FolderId}, користувачу {UserId}", folderId, userId);
                throw;
            }
        }

        public async Task<ApiResponse<IEnumerable<GroupChatRoomDto>>> GetGroupChatsWithoutFolderAsync(int userId)
        {
            try
            {
                var chats = await _chatRoomRepository.GetGroupChatsWithoutFolderAsync(userId);
                var chatIds = chats.Select(chat => chat.Id).ToList();
                var dictionary = await _chatRoomRepository.GetLastMessagePreviewBatchAsync(chatIds);

                foreach (var chat in chats)
                {
                    if (dictionary.TryGetValue(chat.Id, out var lastMessage))
                    {
                        chat.LastMessagePreview = lastMessage;
                    }
                }
                return ApiResponse<IEnumerable<GroupChatRoomDto>>.Ok(chats);
            }
            catch (EntityNotFoundException ex)
            {
                _logger.LogWarning("Користувач {UserId} запросив неіснуючі групові чати без папки", userId);
                return ApiResponse<IEnumerable<GroupChatRoomDto>>.Fail(ex.Message);
            }
            catch (ForbiddenAccessException)
            {
                _logger.LogWarning("Відмовлено в доступі до групових чатів без папки, користувачу {UserId}", userId);
                throw;
            }
        }

        // Методи для групових чатів CREATE
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
            catch (EntityNotFoundException ex)
            {
                _logger.LogWarning("Користувач {UserId} не зміг створити груповий чат", userId);
                return ApiResponse<GroupChatRoomDto>.Fail(ex.Message);
            }
            catch (ForbiddenAccessException)
            {
                _logger.LogWarning("Відмовлено в доступі створення групового чату користувачу {UserId}", userId);
                throw;
            }
        }

        // Методи для групових чатів DELETE
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
                _logger.LogWarning("Користувач {UserId} не зміг видалити груповий чат {ChatRoomId}", userId, chatRoomId);
                return ApiResponse<bool>.Fail(ex.Message);
            }
            catch (ForbiddenAccessException)
            {
                _logger.LogWarning("Відмовлено в доступі видалення групового чату {ChatRoomId} користувачу {UserId}", chatRoomId, userId);
                throw;
            }
        }

        // Інше

        public async Task<bool> IsUserInChatAsync(int userId, int chatRoomId)
        {
            try
            {
                return await _chatRoomRepository.UserBelongsToChatAsync(userId, chatRoomId);
            }
            catch (EntityNotFoundException)
            {
                _logger.LogWarning("Користувач {UserId} не належить до чату, бо чату {ChatRoomId} не існує", userId, chatRoomId);
                return false;
            }
            catch (ForbiddenAccessException)
            {
                _logger.LogWarning("Відмовлено в доступі для перевірки чи належить користувач {UserId} до чату {ChatRoomId}", userId, chatRoomId);
                throw;
            }
        }
    }
}
