using MassTransit;
using MessageService.Data;
using MessageServiceDTOs;
using Shared.Contracts;
using MessageService.Repositories.Interfaces;
using Shared.Exceptions;
using MessageService.Authorization;
using Shared.Responses;
using MessageService.Services.Interfaces;
using System.Security.Claims;
using Shared.Sagas;

namespace MessageService.Services
{
    public class MessageService : IMessageService
    {
        private readonly IEventPublisher _eventPublisher;
        private readonly IChatGrpcClient _chatGrpcClient;
        private readonly IMessageRepository _messageRepository;
        private readonly ILogger<MessageService> _logger;
        private readonly IMessageAuthorizationService _authService;

        public MessageService(
            IHttpClientFactory httpClientFactory,
            IEventPublisher eventPublisher,
            IChatGrpcClient chatGrpcClient,
            IMessageRepository messageRepository,
            IMessageAuthorizationService authService,
            ILogger<MessageService> logger)
        {
            _eventPublisher = eventPublisher;
            _chatGrpcClient = chatGrpcClient;
            _messageRepository = messageRepository;
            _authService = authService;
            _logger = logger;
        }
        
        // Надсилає повідомлення з використанням саги для забезпечення надійної доставки
        public async Task<ApiResponse<MessageDto>> SendMessageViaSagaAsync(SendMessageDto model, int userId)
        {
            try
            {
                // Перевіряємо доступ до чату через gRPC
                await _authService.EnsureCanAccessChatRoomAsync(userId, model.ChatRoomId);

                // Запускаємо сагу для обробки надсилання повідомлення
                var correlationId = Guid.NewGuid();

                // Створюємо тимчасове повідомлення та отримуємо його ID через репозиторій
                var tempMessageDto = await _messageRepository.CreateMessageForSagaAsync(model, userId, correlationId);

                // Публікуємо подію початку відправки повідомлення через RabbitMQ
                await _eventPublisher.PublishAsync(new MessageDeliveryStartedEvent
                {
                    CorrelationId = correlationId,
                    MessageId = tempMessageDto.Id,
                    ChatRoomId = model.ChatRoomId,
                    SenderUserId = userId,
                    Content = model.Content
                });

                return ApiResponse<MessageDto>.Ok(tempMessageDto, "Повідомлення відправлено і буде доставлено отримувачам");
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
                _logger.LogError(ex, "Несподівана помилка при надсиланні повідомлення для чату {ChatRoomId}", model.ChatRoomId);
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

                // Публікуємо подію про зміну стану повідомлення через RabbitMQ
                await _eventPublisher.PublishAsync(new MessageUpdatedEvent
                {
                    Id = messageId,
                    ChatRoomId = messageDto.ChatRoomId,
                    IsRead = true,
                    ReadAt = DateTime.UtcNow
                });

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
                await _eventPublisher.PublishAsync(new MessageDeletedEvent
                {
                    Id = messageId,
                    ChatRoomId = messageDto.ChatRoomId,
                    DeletedByUserId = userId,
                    DeletedAt = DateTime.UtcNow
                });

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

                // Публікуємо команду через RabbitMQ
                await _eventPublisher.PublishAsync(new DeleteAllChatMessagesCommand
                {
                    CorrelationId = correlationId,
                    ChatRoomId = chatRoomId,
                    InitiatedByUserId = userId
                });

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

                if (message.CorrelationId == null)
                {
                    return ApiResponse<bool>.Fail("Повідомлення не належить жодній сазі доставки");
                }

                await _eventPublisher.PublishAsync(new MessageDeliveredToUserEvent
                {
                    CorrelationId = message.CorrelationId.Value,
                    MessageId = messageId,
                    UserId = userId
                });

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
    }
}