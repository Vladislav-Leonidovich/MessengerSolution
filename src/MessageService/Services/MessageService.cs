using MassTransit;
using MessageService.Data;
using MessageServiceDTOs;
using MessageService.Models;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts;
using EncryptionServiceDTOs;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using MessageService.Repositories.Interfaces;
using Shared.Exceptions;
using MessageService.Authorization;
using Shared.Responses;

namespace MessageService.Services
{
    public class MessageService : IMessageService
    {
        private readonly MessageDbContext _dbContext;
        private readonly IBus _bus;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly HttpClient _chatClient;
        private readonly IMessageRepository _messageRepository;
        private readonly ILogger<MessageService> _logger;
        private readonly IMessageAuthorizationService _authService;

        public MessageService(
            MessageDbContext dbContext,
            IPublishEndpoint publishEndpoint,
            IHttpClientFactory httpClientFactory,
            IHttpContextAccessor httpContextAccessor,
            IBus bus, IMessageRepository messageRepository,
            IMessageAuthorizationService authService,
            ILogger<MessageService> logger)
        {
            _dbContext = dbContext;
            _bus = bus;
            _chatClient = httpClientFactory.CreateClient("ChatClient");
            _httpContextAccessor = httpContextAccessor;
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

                // Публікуємо подію MessageCreatedEvent
                var eventMessage = new MessageCreatedEvent
                {
                    Id = messageDto.Id,
                    ChatRoomId = messageDto.ChatRoomId,
                    ChatRoomType = messageDto.ChatRoomType,
                    SenderUserId = messageDto.SenderUserId,
                    Content = messageDto.Content,
                    IsRead = messageDto.IsRead,
                    ReadAt = messageDto.ReadAt,
                    CreatedAt = messageDto.CreatedAt,
                    IsEdited = messageDto.IsEdited,
                    EditedAt = messageDto.EditedAt
                };

                await _bus.Publish(eventMessage);

                return ApiResponse<MessageDto>.Ok(messageDto);
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
                _logger.LogWarning(ex, "Помилка при надсиланні повідомлення для чату {ChatRoomId}", model.ChatRoomId);
                throw;
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
