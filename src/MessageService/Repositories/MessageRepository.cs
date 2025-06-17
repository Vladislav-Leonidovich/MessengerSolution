using System.Text.Json;
using MassTransit;
using MessageService.Data;
using MessageService.Models;
using MessageService.Repositories.Interfaces;
using MessageService.Services;
using MessageService.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts;
using Shared.DTOs.Common;
using Shared.DTOs.Message;
using Shared.DTOs.Responses;
using Shared.Exceptions;
using Shared.Outbox;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MessageService.Repositories
{
    public class MessageRepository : IMessageRepository
    {
        private readonly MessageDbContext _context;
        private readonly ILogger<MessageRepository> _logger;
        private readonly IEncryptionGrpcClient _encryptionClient;
        public MessageRepository(MessageDbContext context, ILogger<MessageRepository> logger, IEncryptionGrpcClient encryptionClient)
        {
            _context = context;
            _logger = logger;
            _encryptionClient = encryptionClient;
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
                var messageCount = await _context.Messages
                    .Where(m => m.ChatRoomId == chatRoomId)
                    .CountAsync();

                if (messageCount == 0)
                {
                    _logger.LogWarning("No messages found for chat room {ChatRoomId}", chatRoomId);
                    return Enumerable.Empty<MessageDto>();
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
                    throw new EntityNotFoundException("Messages", chatRoomId);
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

        public async Task<MessageDto> CreateMessageAsync(string content, int userId, Guid correlationId, int chatRoomId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Знаходимо повідомлення, яке вже було створено
                var existingMessage = await _context.Messages
                    .FirstOrDefaultAsync(m => m.CorrelationId == correlationId);

                if (existingMessage != null)
                {
                    // Повертаємо існуюче повідомлення
                    /*string decryptedContent;
                    try
                    {
                        decryptedContent = await _encryptionClient.DecryptAsync(existingMessage.Content);
                    }
                    catch (ServiceUnavailableException)
                    {
                        decryptedContent = "Повідомлення недоступне для відображення";
                    }*/

                    await transaction.CommitAsync();

                    return new MessageDto()
                    {
                        Id = existingMessage.Id,
                        ChatRoomId = existingMessage.ChatRoomId,
                        SenderUserId = existingMessage.SenderUserId,
                        Content = existingMessage.Content,
                        CreatedAt = existingMessage.CreatedAt,
                        IsRead = existingMessage.IsRead,
                        ReadAt = existingMessage.ReadAt,
                        IsEdited = existingMessage.IsEdited,
                        EditedAt = existingMessage.EditedAt
                    };
                }

                // 1. Шифруємо вміст повідомлення через gRPC
                string encryptedContent;
                try
                {
                    encryptedContent = await _encryptionClient.EncryptAsync(content);
                }
                catch (ServiceUnavailableException)
                {
                    _logger.LogWarning("Сервіс шифрування недоступний. Повідомлення буде збережено без шифрування.");
                    encryptedContent = content;
                }

                // 2. Створюємо нове повідомлення
                var message = new Message
                {
                    ChatRoomId = chatRoomId,
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

                return new MessageDto()
                {
                    Id = message.Id,
                    ChatRoomId = message.ChatRoomId,
                    SenderUserId = message.SenderUserId,
                    Content = content,
                    CreatedAt = message.CreatedAt,
                    IsRead = message.IsRead,
                    ReadAt = message.ReadAt,
                    IsEdited = message.IsEdited,
                    EditedAt = message.EditedAt
                };
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Message sending error for chat room {ChatRoomId}", chatRoomId);
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
                    EditedAt = message.EditedAt
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

                if (lastMessage == null)
                {
                    _logger.LogWarning("No messages found for chat room {ChatRoomId}", chatRoomId);
                    throw new EntityNotFoundException("Messages", chatRoomId);
                }

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
                    SenderUserId = lastMessage.SenderUserId,
                    Content = decryptedContent,
                    CreatedAt = lastMessage.CreatedAt,
                    IsRead = lastMessage.IsRead,
                    ReadAt = lastMessage.ReadAt,
                    IsEdited = lastMessage.IsEdited,
                    EditedAt = lastMessage.EditedAt
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
                    _logger.LogWarning("Повідомлення {MessageId} не знайдено для видалення", messageId);
                    throw new EntityNotFoundException("Message", messageId);
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

        public async Task<(bool Success, int DeletedCount)> DeleteAllMessagesByChatRoomIdAsync(int chatRoomId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Перевіряємо, чи вже було виконано операцію видалення
                var processedEventKey = $"AllMessagesDeleted_{chatRoomId}";
                var existingEvent = await _context.ProcessedEvents
                    .FirstOrDefaultAsync(p => p.EventType == processedEventKey);

                if (existingEvent != null)
                {
                    _logger.LogInformation("Операція видалення всіх повідомлень для чату {ChatRoomId} " +
                        "вже була оброблена", chatRoomId);

                    // Можна зберігати кількість видалених повідомлень в EventData як JSON
                    int previousCount = 0;
                    if (!string.IsNullOrEmpty(existingEvent.EventData))
                    {
                        previousCount = JsonSerializer.Deserialize<int>(existingEvent.EventData);
                    }

                    return (true, previousCount);
                }

                var messages = await _context.Messages
                    .Where(m => m.ChatRoomId == chatRoomId)
                    .ToListAsync();

                int count = messages.Count;

                if (count > 0)
                {
                    _context.Messages.RemoveRange(messages);
                }

                // Зберігаємо запис для забезпечення ідемпотентності
                // І додаємо кількість видалених повідомлень для майбутніх запитів
                string eventData = JsonSerializer.Serialize(count);
                _context.ProcessedEvents.Add(new ProcessedEvent
                {
                    EventId = Guid.NewGuid(),
                    EventType = processedEventKey,
                    ProcessedAt = DateTime.UtcNow,
                    EventData = eventData // Збереження додаткових даних
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Успішно видалено {Count} повідомлень з чату {ChatRoomId}",
                    count, chatRoomId);

                return (true, count);
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Помилка бази даних при видаленні повідомлень для чату {ChatRoomId}", chatRoomId);
                throw new DatabaseException("Помилка при доступі до бази даних", ex);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Непередбачена помилка при видаленні повідомлень для чату {ChatRoomId}", chatRoomId);
                throw;
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
                    SenderUserId = message.SenderUserId,
                    Content = decryptedContent,
                    CreatedAt = message.CreatedAt,
                    IsRead = message.IsRead,
                    ReadAt = message.ReadAt,
                    IsEdited = message.IsEdited,
                    EditedAt = message.EditedAt
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

                if (message == null)
                {
                    _logger.LogWarning("Message with CorrelationId {CorrelationId} not found", correlationId);
                    return null; // Повертаємо null, якщо повідомлення не знайдено
                }

                // Створюємо DTO
                return new MessageDto
                {
                    Id = message.Id,
                    ChatRoomId = message.ChatRoomId,
                    SenderUserId = message.SenderUserId,
                    Content = message.Content,
                    CreatedAt = message.CreatedAt,
                    IsRead = message.IsRead,
                    ReadAt = message.ReadAt,
                    IsEdited = message.IsEdited,
                    EditedAt = message.EditedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при пошуку повідомлення за CorrelationId {CorrelationId}", correlationId);
                throw new DatabaseException("Помилка при доступі до бази даних", ex);
            }
        }

        public async Task<int> GetUserIdSenderMessageAsync(int messageId)
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
                return message.SenderUserId;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error checking sender user ID for message {MessageId}", messageId);
                throw new DatabaseException("Error accessing the database", ex);
            }
        }

        public async Task<Guid?> GetCorrelationIdByMessageIdAsync(int messageId)
        {
            try
            {
                var message = await _context.Messages
                    .Where(m => m.Id == messageId)
                    .Select(m => new { m.CorrelationId })
                    .FirstOrDefaultAsync();

                return message?.CorrelationId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при отриманні CorrelationId для повідомлення {MessageId}", messageId);
                throw new DatabaseException("Помилка при доступі до бази даних", ex);
            }
        }

        public async Task UpdateMessageStatusAsync(int messageId, MessageStatus status)
        {
            try
            {
                var message = await _context.Messages.FindAsync(messageId);
                if (message == null)
                {
                    _logger.LogWarning("Повідомлення з ID {MessageId} не знайдено при оновленні статусу", messageId);
                    return;
                }

                // Перевіряємо, чи є сенс оновлювати статус (тільки "вищий" статус)
                if ((int)status > (int)message.Status)
                {
                    message.Status = status;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Статус повідомлення {MessageId} оновлено до {Status}", messageId, status);
                }
                else
                {
                    _logger.LogDebug("Пропущено оновлення статусу для повідомлення {MessageId}: поточний {CurrentStatus}, запропонований {ProposedStatus}",
                        messageId, message.Status, status);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при оновленні статусу повідомлення {MessageId} до {Status}", messageId, status);
                throw new DatabaseException("Помилка при доступі до бази даних", ex);
            }
        }

        public async Task UpdateMessageStatusByCorrelationIdAsync(Guid correlationId, MessageStatus status)
        {
            try
            {
                var message = await _context.Messages
                    .FirstOrDefaultAsync(m => m.CorrelationId == correlationId);

                if (message == null)
                {
                    _logger.LogWarning("Повідомлення з CorrelationId {CorrelationId} не знайдено при оновленні статусу", correlationId);
                    return;
                }

                // Перевіряємо, чи є сенс оновлювати статус (тільки "вищий" статус)
                if ((int)status > (int)message.Status)
                {
                    message.Status = status;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Статус повідомлення {MessageId} з CorrelationId {CorrelationId} оновлено до {Status}",
                        message.Id, correlationId, status);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при оновленні статусу повідомлення з CorrelationId {CorrelationId} до {Status}",
                    correlationId, status);
                throw new DatabaseException("Помилка при доступі до бази даних", ex);
            }
        }

        public async Task AddToOutboxAsync(string eventType, object eventData)
        {
            var outboxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                EventType = eventType,
                EventData = JsonSerializer.Serialize(eventData),
                CreatedAt = DateTime.UtcNow,
                Status = OutboxMessageStatus.Pending
            };

            _context.OutboxMessages.Add(outboxMessage);
            await _context.SaveChangesAsync();
        }

        public async Task<Dictionary<int, MessageDto>> GetLastMessagePreviewBatchByChatRoomIdAsync(IEnumerable<int> chatRoomIds)
        {
            try
            {
                // Створюємо словник для збереження зв'язку chatRoomId -> Task<MessageDto>
                var taskDictionary = chatRoomIds.ToDictionary(
                    id => id,
                    id => GetLastMessagePreviewByChatRoomIdAsync(id)
                );

                // Чекаємо завершення всіх завдань
                await Task.WhenAll(taskDictionary.Values);

                // Формуємо результат
                var result = new Dictionary<int, MessageDto>();
                foreach (var pair in taskDictionary)
                {
                    result[pair.Key] = await pair.Value;
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при отриманні останніх повідомлень для пакетів чатів");
                throw new DatabaseException("Помилка при доступі до бази даних", ex);
            }
        }
    }
}
