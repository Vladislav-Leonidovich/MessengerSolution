using MassTransit;
using MessageService.Data;
using MessageServiceDTOs;
using Shared.Contracts;
using MessageService.Repositories.Interfaces;
using Shared.Exceptions;
using MessageService.Authorization;
using Shared.Responses;
using MessageService.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Shared.Sagas;
using MessageService.Models;
using Polly;

namespace MessageService.Services
{
    public class MessageService : IMessageService
    {
        private readonly IEventPublisher _eventPublisher;
        private readonly HttpClient _chatClient;
        private readonly IMessageRepository _messageRepository;
        private readonly ILogger<MessageService> _logger;
        private readonly IMessageAuthorizationService _authService;
        private static int _tempMessageIdCounter = 0;
        public MessageService(
            IHttpClientFactory httpClientFactory,
            IHttpContextAccessor httpContextAccessor,
            IEventPublisher eventPublisher,
            IMessageRepository messageRepository,
            IMessageAuthorizationService authService,
            ILogger<MessageService> logger)
        {
            _eventPublisher = eventPublisher;
            _chatClient = httpClientFactory.CreateClient("ChatClient");
            _messageRepository = messageRepository;
            _authService = authService;
            _logger = logger;
        }

        public async Task<ApiResponse<MessageDto>> SendMessageAsync(SendMessageDto model, int userId)
        {
            try
            {
                await _authService.EnsureCanAccessChatRoomAsync(model.ChatRoomId, userId);

                var messageDto = await _messageRepository.CreateMessageByUserIdAsync(model, userId);

                if (messageDto == null)
                {
                    throw new Exception("Не вдалося створити повідомлення.");
                }

                return ApiResponse<MessageDto>.Ok(messageDto);
            }
            catch (EntityNotFoundException ex)
            {
                _logger.LogWarning(ex.Message);
                return ApiResponse<MessageDto>.Fail(ex.Message);
            }
            catch (ForbiddenAccessException ex)
            {
                _logger.LogWarning(ex.Message);
                return ApiResponse<MessageDto>.Fail(ex.Message, new List<string> { "Доступ заборонено" });
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex.Message);
                return ApiResponse<MessageDto>.Fail(ex.Message, ex.Errors.Values.SelectMany(e => e).ToList());
            }
            catch (DatabaseException ex)
            {
                _logger.LogError(ex, "Помилка бази даних при надсиланні повідомлення");
                return ApiResponse<MessageDto>.Fail("Помилка при роботі з базою даних");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Неочікувана помилка при надсиланні повідомлення для чату {ChatRoomId}", model.ChatRoomId);
                return ApiResponse<MessageDto>.Fail("Сталася внутрішня помилка сервера");
            }
        }

        public async Task<ApiResponse<MessageDto>> SendMessageViaSagaAsync(SendMessageDto model, int userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Перевіряємо доступ до чату
                await _authService.EnsureCanAccessChatRoomAsync(model.ChatRoomId, userId);

                // Створюємо тимчасове повідомлення в базі з базовою інформацією
                var message = new Message
                {
                    ChatRoomId = model.ChatRoomId,
                    ChatRoomType = model.ChatRoomType,
                    SenderUserId = userId,
                    Content = "Відправляється...", // Тимчасовий заповнювач
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Messages.AddAsync(message);
                await _context.SaveChangesAsync();

                // Отримуємо реальний ID повідомлення
                int messageId = message.Id;

                // Створюємо DTO для відповіді клієнту
                var messageDto = new MessageDto
                {
                    Id = messageId,
                    ChatRoomId = model.ChatRoomId,
                    ChatRoomType = model.ChatRoomType,
                    SenderUserId = userId,
                    Content = model.Content, // Показуємо необроблений текст у відповіді
                    CreatedAt = message.CreatedAt
                };

                // Запускаємо сагу
                var correlationId = Guid.NewGuid();
                await _eventPublisher.PublishAsync(new MessageDeliveryStartedEvent
                {
                    CorrelationId = correlationId,
                    MessageId = messageId,
                    ChatRoomId = model.ChatRoomId,
                    ChatRoomType = model.ChatRoomType,
                    SenderUserId = userId,
                    Content = model.Content
                });

                await transaction.CommitAsync();
                return ApiResponse<MessageDto>.Ok(messageDto, "Повідомлення відправлено");
            }
            catch (EntityNotFoundException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(ex.Message);
                return ApiResponse<MessageDto>.Fail(ex.Message);
            }
            catch (ForbiddenAccessException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(ex.Message);
                return ApiResponse<MessageDto>.Fail(ex.Message, new List<string> { "Доступ заборонено" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Несподівана помилка при надсиланні повідомлення для чату {ChatRoomId}", model.ChatRoomId);
                return ApiResponse<MessageDto>.Fail("Сталася внутрішня помилка сервера");
            }
        }


        public async Task<ApiResponse<IEnumerable<MessageDto>>> GetMessagesAsync(int chatRoomId, int startIndex, int count, int userId)
        {
            try
            {
                await _authService.EnsureCanAccessChatRoomAsync(chatRoomId, userId);
                var messages = await _messageRepository.GetMessagesByChatRoomIdAsync(chatRoomId, startIndex, count);
                return ApiResponse<IEnumerable<MessageDto>>.Ok(messages);
            }
            catch (EntityNotFoundException ex)
            {
                _logger.LogWarning(ex.Message);
                return ApiResponse<IEnumerable<MessageDto>>.Fail(ex.Message);
            }
            catch (ForbiddenAccessException ex)
            {
                // Логування помилки доступу
                _logger.LogWarning(ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                // Логування помилки
                _logger.LogWarning(ex, "Помилка при отриманні повідомлень для чату {ChatRoomId}", chatRoomId);
                throw;
            }
        }

        public async Task<ApiResponse<int>> GetMessagesCountByChatRoomIdAsync(int chatRoomId)
        {
            try
            {
                var count = await _messageRepository.GetMessagesCountByChatRoomIdAsync(chatRoomId);
                return ApiResponse<int>.Ok(count);
            }
            catch (EntityNotFoundException ex)
            {
                _logger.LogWarning(ex.Message);
                return ApiResponse<int>.Fail(ex.Message);
            }
            catch (ForbiddenAccessException ex)
            {
                // Логування помилки доступу
                _logger.LogWarning(ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                // Логування помилки
                _logger.LogWarning(ex, "Помилка при отриманні кількості повідомлень для чату {ChatRoomId}", chatRoomId);
                throw;
            }
        }

        public async Task<ApiResponse<MessageDto>> MarkMessageAsRead(int messageId)
        {
            try
            {
                var message = await _messageRepository.MarkMessageAsReadByIdAsync(messageId);
                return ApiResponse<MessageDto>.Ok(message);
            }
            catch (EntityNotFoundException ex)
            {
                _logger.LogWarning(ex.Message);
                return ApiResponse<MessageDto>.Fail(ex.Message);
            }
            catch (ForbiddenAccessException ex)
            {
                // Логування помилки доступу
                _logger.LogWarning(ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                // Логування помилки
                _logger.LogWarning(ex, "Помилка при позначенні повідомлення {MessageId} як прочитане", messageId);
                throw;
            }
        }

        public async Task<ApiResponse<MessageDto>> GetLastMessagePreviewByChatRoomIdAsync(int chatRoomId)
        {
            try
            {
                var message = await _messageRepository.GetLastMessagePreviewByChatRoomIdAsync(chatRoomId);
                return ApiResponse<MessageDto>.Ok(message);
            }
            catch (EntityNotFoundException ex)
            {
                _logger.LogWarning(ex.Message);
                return ApiResponse<MessageDto>.Fail(ex.Message);
            }
            catch (ForbiddenAccessException ex)
            {
                // Логування помилки доступу
                _logger.LogWarning(ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                // Логування помилки
                _logger.LogWarning(ex, "Помилка при отриманні останнього повідомлення для чату {ChatRoomId}", chatRoomId);
                throw;
            }
        }

        public async Task<ApiResponse<bool>> DeleteMessageAsync(int messageId)
        {
            try
            {
                var message = await _messageRepository.GetMessageByIdAsync(messageId);
                await _authService.EnsureCanAccessChatRoomAsync(message.ChatRoomId, message.SenderUserId);
                await _messageRepository.DeleteMessageByIdAsync(messageId);
                return ApiResponse<bool>.Ok(true);
            }
            catch (EntityNotFoundException ex)
            {
                _logger.LogWarning(ex.Message);
                return ApiResponse<bool>.Fail(ex.Message);
            }
            catch (ForbiddenAccessException ex)
            {
                // Логування помилки доступу
                _logger.LogWarning(ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                // Логування помилки
                _logger.LogWarning(ex, "Помилка при видаленні повідомлення {MessageId}", messageId);
                throw;
            }
        }


        // Допрацювати метод, додати правильну перевірку наявності повідомлень у чаті
        public async Task<ApiResponse<bool>> DeleteMessagesByChatRoomIdAsync(int chatRoomId)
        {
            try
            {
                var messages = await _messageRepository.GetMessagesByChatRoomIdAsync(chatRoomId, 0, 0);
                if (messages == null || !messages.Any())
                {
                    return ApiResponse<bool>.Ok(true);
                }
                foreach (var message in messages)
                {
                    await _authService.EnsureCanAccessChatRoomAsync(message.ChatRoomId, message.SenderUserId);
                }
                await _messageRepository.DeleteAllMessagesByChatRoomIdAsync(chatRoomId);
                return ApiResponse<bool>.Ok(true);
            }
            catch (EntityNotFoundException ex)
            {
                _logger.LogWarning(ex.Message);
                return ApiResponse<bool>.Fail(ex.Message);
            }
            catch (ForbiddenAccessException ex)
            {
                // Логування помилки доступу
                _logger.LogWarning(ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                // Логування помилки
                _logger.LogWarning(ex, "Помилка при видаленні повідомлень для чату {ChatRoomId}", chatRoomId);
                throw;
            }
        }

        public async Task<bool> IsAuthUserInChatRoomsAsync(int chatRoomId)
        {
            var response = await _chatClient.GetFromJsonAsync<bool>($"api/chat/get-auth-user-in-chat/{chatRoomId}");
            return response;
        }
    }
}
