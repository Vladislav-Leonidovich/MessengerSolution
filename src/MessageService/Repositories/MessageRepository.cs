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
using Shared.Responses;
using Shared.Sagas;

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

                string decryptedContent;
                try
                {
                    decryptedContent = await _encryptionClient.DecryptAsync(message.Content);
                }
                catch (ServiceUnavailableException)
                {
                    _logger.LogWarning("Encryption service unavailable. Message will be displayed as is.");
                    // Якщо сервіс шифрування недоступний, повертаємо зашифрований текст
                    decryptedContent = "Повідомлення недоступне для відображення";
                }

                var messageDto = new MessageDto
                {
                    Id = message.Id,
                    ChatRoomId = message.ChatRoomId,
                    SenderUserId = message.SenderUserId,
                    Content = decryptedContent,
                    CreatedAt = message.CreatedAt,
                    IsRead = message.IsRead,
                    ReadAt = message.ReadAt,
                    IsEdited = message.IsEdited,
                    EditedAt = message.EditedAt,
                    CorrelationId = message.CorrelationId
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
                var messageCount = await _context.Messages
                    .Where(m => m.ChatRoomId == chatRoomId)
                    .CountAsync();

                if (messageCount == 0)
                {
                    return new List<MessageDto>(); // Повертаємо порожній список
                }

                var messages = await _context.Messages
                    .Where(m => m.ChatRoomId == chatRoomId)
                    .OrderByDescending(m => m.CreatedAt)
                    .Skip(startIndex)
                    .Take(count)
                    .ToListAsync();

                if (messages == null)
                {
                    _logger.LogWarning("No messages found for chat room {ChatRoomId}", chatRoomId);
                    throw new EntityNotFoundException("ChatRoom", chatRoomId);
                }

                var encryptedMessages = messages.Select(m => m.Content).ToList();
                List<string> decryptedMessages;

                try
                {
                    decryptedMessages = await _encryptionClient.DecryptBatchAsync(encryptedMessages);
                }
                catch (ServiceUnavailableException)
                {
                    _logger.LogWarning("Encryption service unavailable. Messages will be displayed as is.");
                    // Якщо сервіс шифрування недоступний, повертаємо заглушки
                    decryptedMessages = Enumerable.Repeat("Повідомлення недоступне для відображення", messages.Count).ToList();
                }

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
                        EditedAt = messages[i].EditedAt,
                        CorrelationId = messages[i].CorrelationId,
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

        public async Task<MessageDto> CreateMessageForSagaAsync(SendMessageDto model, int userId, Guid correlationId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Перевіряємо, чи вже було оброблено це повідомлення для забезпечення ідемпотентності
                var existingProcessedEvent = await _context.ProcessedEvents
                    .FirstOrDefaultAsync(p => p.EventId == correlationId &&
                                             p.EventType == "MessageSaved");

                if (existingProcessedEvent != null)
                {
                    // Знаходимо повідомлення, яке вже було створено
                    var existingMessage = await _context.Messages
                        .FirstOrDefaultAsync(m => m.ChatRoomId == model.ChatRoomId &&
                                                 m.SenderUserId == userId &&
                                                 m.CreatedAt > DateTime.UtcNow.AddMinutes(-5));

                    if (existingMessage != null)
                    {
                        // Повертаємо існуюче повідомлення
                        string decryptedContent;
                        try
                        {
                            decryptedContent = await _encryptionClient.DecryptAsync(existingMessage.Content);
                        }
                        catch (ServiceUnavailableException)
                        {
                            decryptedContent = "Повідомлення недоступне для відображення";
                        }

                        await transaction.CommitAsync();

                        return new MessageDto
                        {
                            Id = existingMessage.Id,
                            ChatRoomId = existingMessage.ChatRoomId,
                            ChatRoomType = existingMessage.ChatRoomType,
                            SenderUserId = existingMessage.SenderUserId,
                            Content = decryptedContent,
                            CreatedAt = existingMessage.CreatedAt,
                            IsRead = existingMessage.IsRead,
                            ReadAt = existingMessage.ReadAt,
                            IsEdited = existingMessage.IsEdited,
                            EditedAt = existingMessage.EditedAt,
                            CorrelationId = existingMessage.CorrelationId
                        };
                    }
                }

                // 1. Шифруємо вміст повідомлення через gRPC
                string encryptedContent;
                try
                {
                    encryptedContent = await _encryptionClient.EncryptAsync(model.Content);
                }
                catch (ServiceUnavailableException)
                {
                    _logger.LogWarning("Сервіс шифрування недоступний. Повідомлення буде збережено без шифрування.");
                    encryptedContent = model.Content;
                }

                // 2. Створюємо нове повідомлення
                var message = new Message
                {
                    ChatRoomId = model.ChatRoomId,
                    ChatRoomType = model.ChatRoomType,
                    SenderUserId = userId,
                    Content = encryptedContent,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false,
                    ReadAt = null,
                    IsEdited = false,
                    EditedAt = null,
                    CorrelationId = correlationId
                };

                // 3. Додаємо повідомлення до контексту
                await _context.Messages.AddAsync(message);
                await _context.SaveChangesAsync();

                // 4. Зберігаємо запис для забезпечення ідемпотентності
                var processedEvent = new ProcessedEvent
                {
                    EventId = correlationId,
                    EventType = "MessageSaved",
                    ProcessedAt = DateTime.UtcNow
                };

                await _context.ProcessedEvents.AddAsync(processedEvent);
                await _context.SaveChangesAsync();

                // 5. Фіксуємо транзакцію
                await transaction.CommitAsync();

                // 6. Повертаємо DTO з нешифрованим вмістом для відображення клієнту
                return new MessageDto
                {
                    Id = message.Id,
                    ChatRoomId = message.ChatRoomId,
                    ChatRoomType = message.ChatRoomType,
                    SenderUserId = message.SenderUserId,
                    Content = model.Content, // Оригінальний текст
                    CreatedAt = message.CreatedAt,
                    IsRead = message.IsRead,
                    ReadAt = message.ReadAt,
                    IsEdited = message.IsEdited,
                    EditedAt = message.EditedAt,
                    CorrelationId = message.CorrelationId
                };
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Message sending error for chat room {ChatRoomId}", model.ChatRoomId);
                throw new DatabaseException("Error accessing the database", ex);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Помилка при створенні повідомлення для саги");
                throw new DatabaseException("Помилка при створенні повідомлення", ex);
            }
        }

        // Цей метод створює повідомлення без публікації події (використовується для тестування, потрібно доробити)
        public async Task<MessageDto> UpdateMessageByIdAsync(int messageId, string newContent)
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
                message.Content = newContent;
                message.IsEdited = true;
                message.EditedAt = DateTime.UtcNow;
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
                    EditedAt = message.EditedAt,
                    CorrelationId = message.CorrelationId
                };
                return messageDto;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error updating message with ID {MessageId}", messageId);
                throw new DatabaseException("Error accessing the database", ex);
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
                // Перевірка наявності повідомлень у чаті
                var messageCount = await GetMessagesCountByChatRoomIdAsync(chatRoomId);

                if (messageCount == 0)
                {
                    // Повертаємо порожнє повідомлення для чату без повідомлень
                    return new MessageDto
                    {
                        ChatRoomId = chatRoomId,
                        Content = string.Empty,
                        CreatedAt = DateTime.UtcNow
                    };
                }

                var lastMessage = await _context.Messages
                    .Where(m => m.ChatRoomId == chatRoomId)
                    .OrderByDescending(m => m.CreatedAt)
                    .FirstAsync();

                // Розшифровуємо вміст через gRPC
                string decryptedContent;
                try
                {
                    decryptedContent = await _encryptionClient.DecryptAsync(lastMessage.Content);
                }
                catch (ServiceUnavailableException)
                {
                    _logger.LogWarning("Encryption service unavailable. Message will be displayed as is.");
                    decryptedContent = "Повідомлення недоступне для відображення";
                }

                return new MessageDto
                {
                    Id = lastMessage.Id,
                    ChatRoomId = lastMessage.ChatRoomId,
                    ChatRoomType = lastMessage.ChatRoomType,
                    SenderUserId = lastMessage.SenderUserId,
                    Content = decryptedContent,
                    CreatedAt = lastMessage.CreatedAt,
                    IsRead = lastMessage.IsRead,
                    ReadAt = lastMessage.ReadAt,
                    IsEdited = lastMessage.IsEdited,
                    EditedAt = lastMessage.EditedAt,
                    CorrelationId = lastMessage.CorrelationId,
                };
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Помилка бази даних при отриманні останнього повідомлення для чату {ChatRoomId}", chatRoomId);
                throw new DatabaseException("Помилка при доступі до бази даних", ex);
            }
        }

        public async Task DeleteMessageByIdAsync(int messageId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var message = await _context.Messages
                    .FirstOrDefaultAsync(m => m.Id == messageId);

                if (message == null)
                {
                    // Якщо повідомлення вже видалено, це не помилка
                    _logger.LogInformation("Повідомлення {MessageId} не знайдено для видалення", messageId);
                    return;
                }

                _context.Messages.Remove(message);

                // Зберігаємо запис для забезпечення ідемпотентності
                var processedEvent = new ProcessedEvent
                {
                    EventId = Guid.NewGuid(),
                    EventType = $"MessageDeleted_{messageId}",
                    ProcessedAt = DateTime.UtcNow
                };

                await _context.ProcessedEvents.AddAsync(processedEvent);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation("Повідомлення {MessageId} успішно видалено", messageId);
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Помилка бази даних при видаленні повідомлення {MessageId}", messageId);
                throw new DatabaseException("Помилка при доступі до бази даних", ex);
            }
        }

        public async Task<bool> DeleteAllMessagesByChatRoomIdAsync(int chatRoomId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var messages = await _context.Messages
                    .Where(m => m.ChatRoomId == chatRoomId)
                    .ToListAsync();

                if (messages.Count == 0)
                {
                    _logger.LogInformation("Повідомлень не знайдено для видалення в чаті {ChatRoomId}", chatRoomId);
                    return true; // Немає повідомлень для видалення, вважаємо операцію успішною
                }

                _context.Messages.RemoveRange(messages);

                // Зберігаємо запис для забезпечення ідемпотентності
                var processedEvent = new ProcessedEvent
                {
                    EventId = Guid.NewGuid(),
                    EventType = $"AllMessagesDeleted_{chatRoomId}",
                    ProcessedAt = DateTime.UtcNow
                };

                await _context.ProcessedEvents.AddAsync(processedEvent);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation("Успішно видалено {Count} повідомлень з чату {ChatRoomId}",
                    messages.Count, chatRoomId);

                return true;
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Помилка бази даних при видаленні повідомлень для чату {ChatRoomId}", chatRoomId);
                throw new DatabaseException("Помилка при доступі до бази даних", ex);
            }
        }

        public async Task<MessageDto> MarkMessageAsReadByIdAsync(int messageId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var message = await _context.Messages
                    .FirstOrDefaultAsync(m => m.Id == messageId);

                if (message == null)
                {
                    _logger.LogWarning("Message with ID {MessageId} not found", messageId);
                    throw new EntityNotFoundException("Message", messageId);
                }

                // Позначаємо повідомлення як прочитане
                message.IsRead = true;
                message.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Розшифровуємо вміст через gRPC
                string decryptedContent;
                try
                {
                    decryptedContent = await _encryptionClient.DecryptAsync(message.Content);
                }
                catch (ServiceUnavailableException)
                {
                    _logger.LogWarning("Encryption service unavailable. Message will be displayed as is.");
                    decryptedContent = "Повідомлення недоступне для відображення";
                }

                // Створюємо DTO для відповіді
                var messageDto = new MessageDto
                {
                    Id = message.Id,
                    ChatRoomId = message.ChatRoomId,
                    ChatRoomType = message.ChatRoomType,
                    SenderUserId = message.SenderUserId,
                    Content = decryptedContent,
                    CreatedAt = message.CreatedAt,
                    IsRead = message.IsRead,
                    ReadAt = message.ReadAt,
                    IsEdited = message.IsEdited,
                    EditedAt = message.EditedAt,
                    CorrelationId = message.CorrelationId
                };

                // Зберігаємо запис для забезпечення ідемпотентності
                var processedEvent = new ProcessedEvent
                {
                    EventId = Guid.NewGuid(),
                    EventType = $"MessageMarkedAsRead_{messageId}",
                    ProcessedAt = DateTime.UtcNow
                };

                await _context.ProcessedEvents.AddAsync(processedEvent);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return messageDto;
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Помилка бази даних при позначенні повідомлення {MessageId} як прочитане", messageId);
                throw new DatabaseException("Помилка при доступі до бази даних", ex);
            }
        }

        public async Task<MessageDto?> FindMessageByCorrelationIdAsync(Guid correlationId)
        {
            try
            {
                var message = await _context.Messages
                    .FirstOrDefaultAsync(m => m.CorrelationId == correlationId);

                if(message == null)
                {
                    _logger.LogWarning("Message with CorrelationId {CorrelationId} not found", correlationId);
                    return null; // Повертаємо null, якщо повідомлення не знайдено
                }

                // Створюємо DTO
                return new MessageDto
                {
                    Id = message.Id,
                    ChatRoomId = message.ChatRoomId,
                    ChatRoomType = message.ChatRoomType,
                    SenderUserId = message.SenderUserId,
                    Content = message.Content,
                    CreatedAt = message.CreatedAt,
                    IsRead = message.IsRead,
                    ReadAt = message.ReadAt,
                    IsEdited = message.IsEdited,
                    EditedAt = message.EditedAt,
                    CorrelationId = message.CorrelationId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при пошуку повідомлення за CorrelationId {CorrelationId}", correlationId);
                throw new DatabaseException("Помилка при доступі до бази даних", ex);
            }
        }
    }
}
