using MassTransit;
using MessageService.Data;
using Shared.Contracts;
using MessageService.Repositories.Interfaces;
using Shared.Exceptions;
using MessageService.Authorization;
using MessageService.Services.Interfaces;
using System.Security.Claims;
using Shared.DTOs.Message;
using Shared.DTOs.Responses;
using MessageService.Models;
using MessageService.Sagas.MessageDelivery.Events;
using MessageService.Sagas.DeleteAllMessages.Events;
using MessageService.BackgroundServices;
using MessageService.Hubs;
using Microsoft.AspNetCore.SignalR;
using Polly;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;


namespace MessageService.Services
{
    public class MessageService : IMessageService
    {
        private readonly IChatGrpcClient _chatGrpcClient;
        private readonly IMessageRepository _messageRepository;
        private readonly ILogger<MessageService> _logger;
        private readonly IMessageAuthorizationService _authService;
        private readonly IHubContext<MessageHub> _hubContext;
        private readonly IEncryptionGrpcClient _encryptionClient;

        public MessageService(
            IHttpClientFactory httpClientFactory,
            IChatGrpcClient chatGrpcClient,
            IMessageRepository messageRepository,
            IMessageAuthorizationService authService,
            ILogger<MessageService> logger,
            IHubContext<MessageHub> hubContext,
            IEncryptionGrpcClient encryptionClient)
        {
            _chatGrpcClient = chatGrpcClient;
            _messageRepository = messageRepository;
            _authService = authService;
            _logger = logger;
            _hubContext = hubContext;
            _encryptionClient = encryptionClient;
        }

        // Надсилає повідомлення з використанням саги для забезпечення надійної доставки
        public async Task<ApiResponse<Task>> SendMessageViaSagaAsync(string content, int userId, int chatRoomId)
        {
            try
            {
                // Перевіряємо доступ до чату через gRPC
                await _authService.EnsureCanAccessChatRoomAsync(userId, chatRoomId);

                // Запускаємо сагу для обробки надсилання повідомлення
                var correlationId = Guid.NewGuid();

                var @event = new MessageDeliveryStartedEvent
                {
                    CorrelationId = correlationId,
                    MessageId = -1,
                    ChatRoomId = chatRoomId,
                    SenderUserId = userId,
                    Content = content
                };

                // Публікуємо подію початку відправки повідомлення через Outbox
                await _messageRepository.AddToOutboxAsync(nameof(@event), @event);

                return ApiResponse<Task>.Ok("Повідомлення відправлено і буде доставлено отримувачам");
            }
            catch (EntityNotFoundException ex)
            {
                _logger.LogWarning(ex.Message);
                return ApiResponse<Task>.Fail(ex.Message);
            }
            catch (DatabaseException ex)
            {
                _logger.LogError(ex, "Помилка бази даних при надсиланні повідомлення");
                return ApiResponse<Task>.Fail("Помилка при роботі з базою даних");
            }
            catch (ForbiddenAccessException ex)
            {
                _logger.LogWarning(ex.Message);
                return ApiResponse<Task>.Fail(ex.Message, new List<string> { "Доступ заборонено" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Несподівана помилка при надсиланні повідомлення для чату {ChatRoomId}", chatRoomId);
                return ApiResponse<Task>.Fail("Сталася внутрішня помилка сервера");
            }
        }

        public async Task<ApiResponse<MessageDto>> SendMessageAsync(string content, int userId, int chatRoomId)
        {
            try
            {
                // Перевіряємо доступ до чату через gRPC
                await _authService.EnsureCanAccessChatRoomAsync(userId, chatRoomId);

                var correlationId = Guid.NewGuid();

                // Додаємо повідомлення до репозиторію
                var messageDto = await _messageRepository.CreateMessageAsync(
                    content,
                    userId,
                    correlationId,
                    chatRoomId);

                string groupName = chatRoomId.ToString();
                await _hubContext.Clients.Group(groupName).SendAsync("ReceiveMessage", messageDto);

                return ApiResponse<MessageDto>.Ok(messageDto, "Повідомлення успішно надіслано");
            }
            catch (ServiceUnavailableException)
            {
                // Якщо сервіс шифрування недоступний, показуємо заглушку
                _logger.LogWarning("Сервіс шифрування недоступний. Повідомлення буде надіслано із заглушкою.");
                content = "Повідомлення недоступне для відображення";
                return ApiResponse<MessageDto>.Fail("Помилка при дешифруванні повідомлення");
            }
            catch (EntityNotFoundException ex)
            {
                _logger.LogWarning(ex.Message);
                return ApiResponse<MessageDto>.Fail(ex.Message);
            }
            catch (DatabaseException ex)
            {
                _logger.LogError(ex, "Помилка бази даних при надсиланні повідомлення");
                return ApiResponse<MessageDto>.Fail("Помилка при роботі з базою даних");
            }
            catch (ForbiddenAccessException ex)
            {
                _logger.LogWarning(ex.Message);
                return ApiResponse<MessageDto>.Fail(ex.Message, new List<string> { "Доступ заборонено" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Несподівана помилка при надсиланні повідомлення для чату {ChatRoomId}", chatRoomId);
                return ApiResponse<MessageDto>.Fail("Сталася внутрішня помилка сервера");
            }
        }


        // Отримує повідомлення з чату з підтримкою пагінації
        public async Task<ApiResponse<IEnumerable<MessageDto>>> GetMessagesAsync(int chatRoomId, int userId, int startIndex, int count)
        {
            try
            {
                // Перевіряємо доступ до чату через gRPC
                await _authService.EnsureCanAccessChatRoomAsync(userId, chatRoomId);

                // Отримуємо повідомлення з репозиторію
                var messages = await _messageRepository.GetMessagesByChatRoomIdAsync(chatRoomId, startIndex, count);
                return ApiResponse<IEnumerable<MessageDto>>.Ok(messages);
            }
            catch (EntityNotFoundException ex)
            {
                _logger.LogWarning(ex.Message);
                return ApiResponse<IEnumerable<MessageDto>>.Fail(ex.Message);
            }
            catch (DatabaseException ex)
            {
                _logger.LogError(ex, "Помилка бази даних при отриманні повідомлення");
                return ApiResponse<IEnumerable<MessageDto>>.Fail("Помилка при роботі з базою даних");
            }
            catch (ForbiddenAccessException ex)
            {
                _logger.LogWarning(ex.Message);
                return ApiResponse<IEnumerable<MessageDto>>.Fail(ex.Message, new List<string> { "Доступ заборонено" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при отриманні повідомлень для чату {ChatRoomId}", chatRoomId);
                return ApiResponse<IEnumerable<MessageDto>>.Fail("Сталася внутрішня помилка сервера");
            }
        }


        // Отримує кількість повідомлень у чаті
        public async Task<ApiResponse<int>> GetMessagesCountByChatRoomIdAsync(int chatRoomId, int userId)
        {
            try
            {
                // Перевіряємо існування чату через gRPC
                bool chatExists = await _chatGrpcClient.CheckAccessAsync(userId, chatRoomId);
                if (!chatExists)
                {
                    throw new EntityNotFoundException("ChatRoom", chatRoomId);
                }

                // Отримуємо кількість повідомлень з репозиторію
                var count = await _messageRepository.GetMessagesCountByChatRoomIdAsync(chatRoomId);
                return ApiResponse<int>.Ok(count);
            }
            catch (EntityNotFoundException ex)
            {
                _logger.LogWarning(ex.Message);
                return ApiResponse<int>.Fail(ex.Message);
            }
            catch (DatabaseException ex)
            {
                _logger.LogError(ex, "Помилка бази даних при отриманні кількості повідомлень");
                return ApiResponse<int>.Fail("Помилка при роботі з базою даних");
            }
            catch (ForbiddenAccessException ex)
            {
                _logger.LogWarning(ex.Message);
                return ApiResponse<int>.Fail(ex.Message, new List<string> { "Доступ заборонено" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при отриманні кількості повідомлень для чату {ChatRoomId}", chatRoomId);
                return ApiResponse<int>.Fail("Сталася внутрішня помилка сервера");
            }
        }


        // Позначає повідомлення як прочитане
        public async Task<ApiResponse<MessageDto>> MarkMessageAsRead(int messageId, int userId)
        {
            try
            {
                // Отримуємо повідомлення, щоб перевірити доступ
                var messageDto = await _messageRepository.GetMessageByIdAsync(messageId);

                // Перевіряємо, чи має користувач доступ до цього чату

                if (messageDto.SenderUserId != userId)
                {
                    await _authService.EnsureCanAccessChatRoomAsync(userId, messageDto.ChatRoomId);
                }

                if (messageDto.IsRead)
                {
                    return ApiResponse<MessageDto>.Fail("Повідомлення вже позначене як прочитане");
                }

                // Позначаємо повідомлення як прочитане через репозиторій
                var updatedMessageDto = await _messageRepository.MarkMessageAsReadByIdAsync(messageId);

                var @event = new MessageUpdatedEvent
                {
                    Id = messageId,
                    ChatRoomId = messageDto.ChatRoomId,
                    IsRead = true,
                    ReadAt = DateTime.UtcNow
                };
                // Публікуємо подію про оновлення повідомлення через Outbox
                await _messageRepository.AddToOutboxAsync(nameof(@event), @event);

                return ApiResponse<MessageDto>.Ok(updatedMessageDto, "Повідомлення позначено як прочитане");
            }
            catch (EntityNotFoundException ex)
            {
                _logger.LogWarning(ex.Message);
                return ApiResponse<MessageDto>.Fail(ex.Message);
            }
            catch (DatabaseException ex)
            {
                _logger.LogError(ex, "Помилка бази даних при позначенні повідомлення як прочитане");
                return ApiResponse<MessageDto>.Fail("Помилка при роботі з базою даних");
            }
            catch (ForbiddenAccessException ex)
            {
                _logger.LogWarning(ex.Message);
                return ApiResponse<MessageDto>.Fail(ex.Message, new List<string> { "Доступ заборонено" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при позначенні повідомлення {MessageId} як прочитане", messageId);
                return ApiResponse<MessageDto>.Fail("Сталася внутрішня помилка сервера");
            }
        }


        // Отримує останнє повідомлення з чату для попереднього перегляду
        public async Task<ApiResponse<MessageDto>> GetLastMessagePreviewByChatRoomIdAsync(int chatRoomId, int userId)
        {
            try
            {
                // Перевіряємо існування чату через gRPC
                bool chatExists = await _chatGrpcClient.CheckAccessAsync(userId, chatRoomId);
                if (!chatExists)
                {
                    throw new EntityNotFoundException("ChatRoom", chatRoomId);
                }

                // Отримуємо останнє повідомлення з репозиторію
                var message = await _messageRepository.GetLastMessagePreviewByChatRoomIdAsync(chatRoomId);
                return ApiResponse<MessageDto>.Ok(message);
            }
            catch (EntityNotFoundException ex)
            {
                _logger.LogWarning(ex.Message);
                return ApiResponse<MessageDto>.Fail(ex.Message);
            }
            catch (DatabaseException ex)
            {
                _logger.LogError(ex, "Помилка бази даних при отриманні останнього повідомлення");
                return ApiResponse<MessageDto>.Fail("Помилка при роботі з базою даних");
            }
            catch (ForbiddenAccessException ex)
            {
                _logger.LogWarning(ex.Message);
                return ApiResponse<MessageDto>.Fail(ex.Message, new List<string> { "Доступ заборонено" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при отриманні останнього повідомлення для чату {ChatRoomId}", chatRoomId);
                return ApiResponse<MessageDto>.Fail("Сталася внутрішня помилка сервера");
            }
        }


        // Видаляє повідомлення
        public async Task<ApiResponse<bool>> DeleteMessageAsync(int messageId, int userId)
        {
            try
            {
                // Отримуємо повідомлення, щоб перевірити доступ
                var messageDto = await _messageRepository.GetMessageByIdAsync(messageId);

                // Перевіряємо, чи має користувач право видаляти це повідомлення
                /*if (messageDto.SenderUserId != userId)
                {
                    // Перевіряємо, чи є користувач адміністратором чату через gRPC
                    bool isAdmin = await _chatGrpcClient.CheckAdminAccessAsync(userId, messageDto.ChatRoomId);
                    if (!isAdmin)
                    {
                        throw new ForbiddenAccessException("Лише відправник або адміністратор чату може видаляти повідомлення");
                    }
                }*/

                // Видаляємо повідомлення через репозиторій
                await _messageRepository.DeleteMessageByIdAsync(messageId);

                // Публікуємо подію про видалення повідомлення через RabbitMQ
                var @event = new MessageDeletedEvent
                {
                    Id = messageId,
                    ChatRoomId = messageDto.ChatRoomId,
                    DeletedByUserId = userId,
                    DeletedAt = DateTime.UtcNow
                };

                // Публікуємо подію про видалення повідомлення через Outbox
                await _messageRepository.AddToOutboxAsync(nameof(@event), @event);

                return ApiResponse<bool>.Ok(true, "Повідомлення успішно видалено");
            }
            catch (EntityNotFoundException ex)
            {
                _logger.LogWarning(ex.Message);
                return ApiResponse<bool>.Fail(ex.Message);
            }
            catch (DatabaseException ex)
            {
                _logger.LogError(ex, "Помилка бази даних при видаленні повідомлення");
                return ApiResponse<bool>.Fail("Помилка при роботі з базою даних");
            }
            catch (ForbiddenAccessException ex)
            {
                _logger.LogWarning(ex.Message);
                return ApiResponse<bool>.Fail(ex.Message, new List<string> { "Доступ заборонено" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при видаленні повідомлення {MessageId}", messageId);
                return ApiResponse<bool>.Fail("Сталася внутрішня помилка сервера");
            }
        }


        // Видаляє всі повідомлення з чату за допомогою саги
        public async Task<ApiResponse<bool>> DeleteMessagesByChatRoomIdAsync(int chatRoomId, int userId)
        {
            try
            {
                // Перевіряємо, чи має користувач право видаляти повідомлення з цього чату
                // Зазвичай це може робити лише адміністратор чату
                /*bool isAdmin = await _chatGrpcClient.CheckAdminAccessAsync(userId, chatRoomId);
                if (!isAdmin)
                {
                    throw new ForbiddenAccessException("Лише адміністратор чату може видаляти всі повідомлення");
                }*/

                // Перевіряємо існування чату
                var chatExists = await _chatGrpcClient.CheckAccessAsync(userId, chatRoomId);
                if (!chatExists)
                {
                    throw new EntityNotFoundException("ChatRoom", chatRoomId);
                }

                // Запускаємо сагу для масового видалення повідомлень
                var correlationId = Guid.NewGuid();

                var @event = new DeleteAllChatMessagesCommand
                {
                    CorrelationId = correlationId,
                    ChatRoomId = chatRoomId,
                    InitiatedByUserId = userId
                };

                // Публікуємо подію початку видалення повідомлень через Outbox
                await _messageRepository.AddToOutboxAsync(nameof(@event), @event);

                return ApiResponse<bool>.Ok(true, "Запит на видалення всіх повідомлень прийнято");
            }
            catch (EntityNotFoundException ex)
            {
                _logger.LogWarning(ex.Message);
                return ApiResponse<bool>.Fail(ex.Message);
            }
            catch (DatabaseException ex)
            {
                _logger.LogError(ex, "Помилка бази даних при видаленні повідомлень");
                return ApiResponse<bool>.Fail("Помилка при роботі з базою даних");
            }
            catch (ForbiddenAccessException ex)
            {
                _logger.LogWarning(ex.Message);
                return ApiResponse<bool>.Fail(ex.Message, new List<string> { "Доступ заборонено" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при видаленні повідомлень для чату {ChatRoomId}", chatRoomId);
                return ApiResponse<bool>.Fail("Сталася внутрішня помилка сервера");
            }
        }

        public async Task<ApiResponse<bool>> ConfirmMessageDeliveryAsync(int messageId, int userId)
        {
            try
            {

                var message = await _messageRepository.GetMessageByIdAsync(messageId);
                if (message == null)
                {
                    throw new EntityNotFoundException("Message", messageId);
                }
                var correlationId = await _messageRepository.GetCorrelationIdByMessageIdAsync(messageId);
                if (correlationId == null)
                {
                    return ApiResponse<bool>.Fail("Повідомлення не належить жодній сазі доставки");
                }

                var @event = new MessageDeliveredToUserEvent
                {
                    CorrelationId = correlationId.Value,
                    MessageId = messageId,
                    UserId = userId
                };

                // Публікуємо подію підтвердження доставки через Outbox
                await _messageRepository.AddToOutboxAsync(nameof(@event), @event);

                return ApiResponse<bool>.Ok(true, "Підтвердження доставки надіслано");
            }
            catch (EntityNotFoundException ex)
            {
                _logger.LogWarning(ex.Message);
                return ApiResponse<bool>.Fail(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при підтвердженні доставки повідомлення {MessageId}", messageId);
                return ApiResponse<bool>.Fail("Сталася внутрішня помилка сервера");
            }
        }

        public async Task<ApiResponse<Dictionary<int, MessageDto>>> GetLastMessagesBatchAsync(IEnumerable<int> chatRoomIds, int userId)
        {
            try
            {
                // Перевіряємо доступ до кожного чату через gRPC
                var tasks = chatRoomIds.Select(chatRoomId => _chatGrpcClient.CheckAccessAsync(userId, chatRoomId));
                var accessResults = Task.WhenAll(tasks).Result;

                // Фільтруємо чати, до яких користувач має доступ
                var accessibleChatRoomIds = chatRoomIds.Where((id, index) => accessResults[index]).ToList();
                // Отримуємо останні повідомлення для доступних чатів
                var messages = await _messageRepository.GetLastMessagePreviewBatchByChatRoomIdAsync(accessibleChatRoomIds);
                return ApiResponse<Dictionary<int, MessageDto>>.Ok(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при отриманні останніх повідомлень для чатів");
                return ApiResponse<Dictionary<int, MessageDto>>.Fail("Сталася внутрішня помилка сервера");
            }
        }
    }
}