using System.Text.Json;
using MassTransit;
using MessageService.Data;
using MessageService.Models;
using MessageService.Repositories.Interfaces;
using MessageService.Services.Interfaces;
using MessageServiceDTOs;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts;
using Shared.Exceptions;

namespace MessageService.Repositories
{
    public class MessageRepository : IMessageRepository
    {
        private readonly MessageDbContext _context;
        private readonly ILogger<MessageRepository> _logger;
        private readonly IEncryptionGrpcClient _encryptionClient;
        private readonly IEventPublisher _eventPublisher;
        public MessageRepository(MessageDbContext context, ILogger<MessageRepository> logger, IEncryptionGrpcClient encryptionClient, IEventPublisher eventPublisher)
        {
            _context = context;
            _logger = logger;
            _encryptionClient = encryptionClient;
            _eventPublisher = eventPublisher;
        }

        public async Task<MessageDto> GetMessageByIdAsync(int messageId)
        {
            try
            {
                var message = await _context.Messages
                    .FirstOrDefaultAsync(m => m.Id == messageId);
                if (message == null)
                {
                    _logger.LogWarning("Message with ID {MessageId} not found", messageId);
                    throw new EntityNotFoundException("Message", messageId);
                }
                var decryptedMessage = await _encryptionClient.DecryptAsync(message.Content);
                var messageDto = new MessageDto
                {
                    Id = message.Id,
                    ChatRoomId = message.ChatRoomId,
                    SenderUserId = message.SenderUserId,
                    Content = decryptedMessage,
                    CreatedAt = message.CreatedAt,
                    IsRead = message.IsRead,
                    ReadAt = message.ReadAt,
                    IsEdited = message.IsEdited,
                    EditedAt = message.EditedAt
                };
                return messageDto;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error retrieving message with ID {MessageId}", messageId);
                throw new DatabaseException("Error accessing the database", ex);
            }
        }

        public async Task<IEnumerable<MessageDto>> GetMessagesByChatRoomIdAsync(int chatRoomId, int startIndex, int count)
        {
            try
            {
                var messages = await _context.Messages
                    .Where(m => m.ChatRoomId == chatRoomId)
                    .OrderBy(m => m.CreatedAt)
                    .Skip(startIndex)
                    .Take(count)
                    .ToListAsync();

                if (messages == null || messages.Count == 0)
                {
                    _logger.LogWarning("No messages found for chat room {ChatRoomId}", chatRoomId);
                    throw new EntityNotFoundException("ChatRoom", chatRoomId);
                }

                var encryptedMessages = messages.Select(m => m.Content).ToList();
                var decryptedMessages = await _encryptionClient.DecryptBatchAsync(encryptedMessages);

                var messageDtos = new List<MessageDto>();
                for (int i = 0; i < messages.Count; i++)
                {
                    messageDtos.Add(new MessageDto
                    {
                        Id = messages[i].Id,
                        ChatRoomId = messages[i].ChatRoomId,
                        SenderUserId = messages[i].SenderUserId,
                        Content = decryptedMessages[i],
                        CreatedAt = messages[i].CreatedAt,
                        IsRead = messages[i].IsRead,
                        ReadAt = messages[i].ReadAt,
                        IsEdited = messages[i].IsEdited,
                        EditedAt = messages[i].EditedAt
                    });
                }

                return messageDtos;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error retrieving messages for chat room {ChatRoomId}", chatRoomId);
                throw new DatabaseException("Error accessing the database", ex);
            }
        }

        public async Task<MessageDto> CreateMessageByUserIdAsync(SendMessageDto model, int userId)
        {
            try
            {
                var encryptedMessage = await _encryptionClient.EncryptAsync(model.Content);

                var message = new Message
                {
                    ChatRoomId = model.ChatRoomId,
                    ChatRoomType = model.ChatRoomType,
                    SenderUserId = userId,
                    Content = encryptedMessage,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Messages.AddAsync(message);
                await _context.SaveChangesAsync();

                var messageDto = new MessageDto
                {
                    Id = message.Id,
                    ChatRoomId = message.ChatRoomId,
                    SenderUserId = message.SenderUserId,
                    Content = model.Content,
                    CreatedAt = message.CreatedAt,
                    IsRead = message.IsRead,
                    ReadAt = message.ReadAt,
                    IsEdited = message.IsEdited,
                    EditedAt = message.EditedAt
                };

                return messageDto;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Message sending error for chat room {ChatRoomId}", model.ChatRoomId);
                throw new DatabaseException("Error accessing the database", ex);
            }
        }

        public async Task<MessageDto> CreateMessageWithEventAsync(SendMessageDto model, int userId)
        {
            // Розпочинаємо транзакцію
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Шифруємо вміст повідомлення
                var encryptedMessage = await _encryptionClient.EncryptAsync(model.Content);

                // 2. Створюємо нове повідомлення
                var message = new Message
                {
                    ChatRoomId = model.ChatRoomId,
                    ChatRoomType = model.ChatRoomType,
                    SenderUserId = userId,
                    Content = encryptedMessage,
                    CreatedAt = DateTime.UtcNow
                };

                // 3. Додаємо повідомлення до контексту
                await _context.Messages.AddAsync(message);

                // 4. Зберігаємо зміни
                await _context.SaveChangesAsync();

                // 5. Створюємо івент
                var eventMessage = new MessageCreatedEvent
                {
                    Id = message.Id,
                    ChatRoomId = message.ChatRoomId,
                    ChatRoomType = message.ChatRoomType,
                    SenderUserId = message.SenderUserId,
                    Content = model.Content,
                    CreatedAt = message.CreatedAt,
                    IsRead = message.IsRead,
                    ReadAt = message.ReadAt,
                    IsEdited = message.IsEdited,
                    EditedAt = message.EditedAt
                };

                // 6. Публікуємо івент в тій же транзакції
                await _eventPublisher.PublishInTransactionAsync(eventMessage, transaction);

                // 7. Зберігаємо івент в базі даних
                await _context.SaveChangesAsync();

                // 8. Фіксуємо транзакцію
                await transaction.CommitAsync();

                // 9. Створюємо DTO для повернення клієнту
                var messageDto = new MessageDto
                {
                    Id = message.Id,
                    ChatRoomId = message.ChatRoomId,
                    ChatRoomType = message.ChatRoomType,
                    SenderUserId = message.SenderUserId,
                    Content = model.Content,
                    CreatedAt = message.CreatedAt,
                    IsRead = message.IsRead,
                    ReadAt = message.ReadAt,
                    IsEdited = message.IsEdited,
                    EditedAt = message.EditedAt
                };

                return messageDto;
            }
            catch (Exception ex)
            {
                // Відкат транзакції у випадку помилки
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Помилка при створенні повідомлення та івента для чату {ChatRoomId}",
                    model.ChatRoomId);
                throw new DatabaseException("Помилка при створенні повідомлення", ex);
            }
        }

        public async Task<int> GetMessagesCountByChatRoomIdAsync(int chatRoomId)
        {
            try
            {
                return await _context.Messages
                    .Where(m => m.ChatRoomId == chatRoomId)
                    .CountAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error retrieving message count for chat room {ChatRoomId}", chatRoomId);
                throw new DatabaseException("Error accessing the database", ex);
            }
        }

        public async Task<MessageDto> GetLastMessagePreviewByChatRoomIdAsync(int chatRoomId)
        {
            try
            {
                var lastMessage = await _context.Messages
                .Where(m => m.ChatRoomId == chatRoomId)
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefaultAsync();

                if (lastMessage == null)
                {
                    _logger.LogWarning("No messages found for chat room {ChatRoomId}", chatRoomId);
                    throw new EntityNotFoundException("ChatRoom", chatRoomId);
                }

                var decryptedLastMessage = await _encryptionClient.DecryptAsync(lastMessage.Content);

                var messageDto = new MessageDto
                {
                    Id = lastMessage.Id,
                    ChatRoomId = lastMessage.ChatRoomId,
                    SenderUserId = lastMessage.SenderUserId,
                    Content = decryptedLastMessage,
                    CreatedAt = lastMessage.CreatedAt,
                    IsRead = lastMessage.IsRead,
                    ReadAt = lastMessage.ReadAt,
                    IsEdited = lastMessage.IsEdited,
                    EditedAt = lastMessage.EditedAt
                };

                return messageDto;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error retrieving last message preview for chat room {ChatRoomId}", chatRoomId);
                throw new DatabaseException("Error accessing the database", ex);
            }
        }

        public async Task DeleteMessageByIdAsync(int messageId)
        {
            try
            {
                var message = await _context.Messages
                                    .FirstOrDefaultAsync(m => m.Id == messageId);

                if (message == null)
                {
                    _logger.LogWarning("Message with ID {MessageId} not found", messageId);
                    throw new EntityNotFoundException("Message", messageId);
                }

                _context.Messages.Remove(message);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error deleting message with ID {MessageId}", messageId);
                throw new DatabaseException("Error accessing the database", ex);
            }
        }

        public async Task<bool> DeleteAllMessagesByChatRoomIdAsync(int chatRoomId)
        {
            try
            {
                var messages = await _context.Messages
                    .Where(m => m.ChatRoomId == chatRoomId)
                    .ToListAsync();
                if (messages == null || messages.Count == 0)
                {
                    _logger.LogWarning("No messages found for chat room {ChatRoomId}", chatRoomId);
                    throw new EntityNotFoundException("ChatRoom", chatRoomId);
                }

                _context.Messages.RemoveRange(messages);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error deleting messages for chat room {ChatRoomId}", chatRoomId);
                throw new DatabaseException("Error accessing the database", ex);
            }
        }

        public async Task<MessageDto> MarkMessageAsReadByIdAsync(int messageId)
        {
            try
            {
                var message = await _context.Messages
                                    .FirstOrDefaultAsync(m => m.Id == messageId);
                if (message == null)
                {
                    _logger.LogWarning("Message with ID {MessageId} not found", messageId);
                    throw new EntityNotFoundException("Message", messageId);
                }

                message.IsRead = true;
                message.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                var decryptedMessage = await _encryptionClient.DecryptAsync(message.Content);
                var messageDto = new MessageDto
                {
                    Id = message.Id,
                    ChatRoomId = message.ChatRoomId,
                    SenderUserId = message.SenderUserId,
                    Content = decryptedMessage,
                    CreatedAt = message.CreatedAt,
                    IsRead = message.IsRead,
                    ReadAt = message.ReadAt,
                    IsEdited = message.IsEdited,
                    EditedAt = message.EditedAt
                };
                return messageDto;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error marking message with ID {MessageId} as read", messageId);
                throw new DatabaseException("Error accessing the database", ex);
            }
        }
    }
}
