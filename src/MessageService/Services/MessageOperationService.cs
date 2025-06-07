using MassTransit;
using MessageService.Data;
using MessageService.Models;
using MessageService.Sagas.MessageOperation.Events;
using MessageService.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Shared.DTOs.Message;
using Shared.Exceptions;

namespace MessageService.Services
{
    public class MessageOperationService : IMessageOperationService
    {
        private readonly MessageDbContext _context;
        private readonly IBus _bus;
        private readonly ILogger<MessageOperationService> _logger;

        public MessageOperationService(
            MessageDbContext context,
            IBus bus,
            ILogger<MessageOperationService> logger)
        {
            _context = context;
            _bus = bus;
            _logger = logger;
        }

        // Метод для отримання операції з перевіркою на null
        private async Task<MessageOperation> GetOperationRequiredAsync(Guid correlationId)
        {
            var operation = await _context.MessageOperations
                .FirstOrDefaultAsync(op => op.CorrelationId == correlationId);

            if (operation == null)
            {
                throw new EntityNotFoundException($"Операція з ID {correlationId} не знайдена");
            }

            return operation;
        }

        // Перевірка на можливі конфліктні операції
        private async Task ValidateOperationAsync(MessageOperationType operationType, int? messageId, int? chatRoomId, int userId)
        {
            // Перевіряємо, чи немає активних операцій того ж типу для того ж повідомлення
            if (messageId.HasValue)
            {
                bool hasConflictingOperation = await _context.MessageOperations
                    .AnyAsync(op => op.MessageId == messageId &&
                                   op.OperationType == operationType &&
                                   op.IsActive);

                if (hasConflictingOperation)
                {
                    throw new ConflictException($"Для повідомлення {messageId} вже виконується операція типу {operationType}");
                }
            }

            // Перевіряємо, чи немає активних операцій того ж типу для всього чату
            if (chatRoomId.HasValue)
            {
                bool hasConflictingChatOperation = await _context.MessageOperations
                    .AnyAsync(op => op.ChatRoomId == chatRoomId &&
                                   op.OperationType == operationType &&
                                   op.IsActive);

                if (hasConflictingChatOperation)
                {
                    throw new ConflictException($"Для чату {chatRoomId} вже виконується операція типу {operationType}");
                }
            }
        }

        public async Task<MessageOperation> StartOperationAsync(
            Guid correlationId,
            MessageOperationType operationType,
            int userId,
            int? messageId = null,
            int? chatRoomId = null,
            string? operationData = null)
        {
            try
            {
                // Перевіряємо, чи не існує вже операція з таким CorrelationId
                var existingOperation = await _context.MessageOperations
                    .FirstOrDefaultAsync(op => op.CorrelationId == correlationId);

                if (existingOperation != null)
                {
                    _logger.LogInformation("Операція з CorrelationId {CorrelationId} вже існує. Повертаємо існуючу.",
                        correlationId);
                    return existingOperation;
                }

                // Перевіряємо наявність конфліктних операцій
                await ValidateOperationAsync(operationType, messageId, chatRoomId, userId);

                var operation = new MessageOperation
                {
                    CorrelationId = correlationId,
                    OperationType = operationType,
                    MessageId = messageId,
                    ChatRoomId = chatRoomId,
                    UserId = userId,
                    Status = MessageOperationStatus.Pending,
                    OperationData = operationData,
                    StatusMessage = "Операція створена",
                    CreatedAt = DateTime.UtcNow,
                    LastUpdatedAt = DateTime.UtcNow
                };

                _context.MessageOperations.Add(operation);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Створено операцію {OperationType} для повідомлення {MessageId} в чаті {ChatRoomId} користувачем {UserId}. CorrelationId: {CorrelationId}",
                    operationType, messageId, chatRoomId, userId, correlationId);

                // Публікуємо подію про початок операції
                await _bus.Publish(new MessageOperationStartedEvent
                {
                    CorrelationId = correlationId,
                    OperationType = operationType,
                    MessageId = messageId,
                    ChatRoomId = chatRoomId,
                    UserId = userId,
                    OperationData = operationData
                });

                return operation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при створенні операції {OperationType} для повідомлення {MessageId} в чаті {ChatRoomId}",
                    operationType, messageId, chatRoomId);
                throw;
            }
        }

        public async Task UpdateProgressAsync(Guid correlationId, int progress, string statusMessage)
        {
            try
            {
                var operation = await GetOperationRequiredAsync(correlationId);

                // Валідація прогресу
                if (progress < 0 || progress > 100)
                {
                    throw new ArgumentException($"Прогрес повинен бути в діапазоні 0-100, отримано: {progress}");
                }

                // Оновлюємо статус на InProgress, якщо він ще Pending
                if (operation.Status == MessageOperationStatus.Pending)
                {
                    operation.Status = MessageOperationStatus.InProgress;
                    operation.StartedAt = DateTime.UtcNow;
                }

                operation.Progress = progress;
                operation.StatusMessage = statusMessage;
                operation.LastUpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Оновлено прогрес операції {CorrelationId}: {Progress}% - {StatusMessage}",
                    correlationId, progress, statusMessage);

                // Публікуємо подію про оновлення прогресу
                await _bus.Publish(new MessageOperationProgressEvent
                {
                    CorrelationId = correlationId,
                    Progress = progress,
                    StatusMessage = statusMessage
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при оновленні прогресу операції {CorrelationId}", correlationId);
                throw;
            }
        }

        public async Task CompleteOperationAsync(Guid correlationId, string? result = null)
        {
            try
            {
                var operation = await GetOperationRequiredAsync(correlationId);

                // Перевіряємо, чи не завершена вже операція
                if (operation.IsCompleted)
                {
                    _logger.LogWarning("Спроба завершити вже завершену операцію {CorrelationId}", correlationId);
                    return;
                }

                operation.Status = MessageOperationStatus.Completed;
                operation.Progress = 100;
                operation.StatusMessage = "Операція успішно завершена";
                operation.Result = result;
                operation.CompletedAt = DateTime.UtcNow;
                operation.LastUpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Операція {CorrelationId} успішно завершена", correlationId);

                // Публікуємо подію про завершення операції
                await _bus.Publish(new MessageOperationCompletedEvent
                {
                    CorrelationId = correlationId,
                    Result = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при завершенні операції {CorrelationId}", correlationId);
                throw;
            }
        }

        public async Task FailOperationAsync(Guid correlationId, string errorMessage, string? errorCode = null)
        {
            try
            {
                var operation = await GetOperationRequiredAsync(correlationId);

                // Перевіряємо, чи не завершена вже операція
                if (operation.IsCompleted)
                {
                    _logger.LogWarning("Спроба помітити як невдалу вже завершену операцію {CorrelationId}", correlationId);
                    return;
                }

                operation.Status = MessageOperationStatus.Failed;
                operation.StatusMessage = "Операція завершилась з помилкою";
                operation.ErrorMessage = errorMessage;
                operation.ErrorCode = errorCode;
                operation.CompletedAt = DateTime.UtcNow;
                operation.LastUpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogWarning("Операція {CorrelationId} завершилась з помилкою: {ErrorMessage}", correlationId, errorMessage);

                // Публікуємо подію про невдачу операції
                await _bus.Publish(new MessageOperationFailedEvent
                {
                    CorrelationId = correlationId,
                    ErrorMessage = errorMessage,
                    ErrorCode = errorCode
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при обробці невдачі операції {CorrelationId}", correlationId);
                throw;
            }
        }

        public async Task CancelOperationAsync(Guid correlationId, string reason)
        {
            try
            {
                var operation = await GetOperationRequiredAsync(correlationId);

                // Перевіряємо, чи не завершена вже операція
                if (operation.IsCompleted)
                {
                    _logger.LogWarning("Спроба скасувати вже завершену операцію {CorrelationId}", correlationId);
                    return;
                }

                // Перевіряємо, чи можна скасувати операцію
                if (!operation.CanBeCancelled)
                {
                    throw new InvalidOperationException($"Операцію зі статусом {operation.Status} не можна скасувати");
                }

                operation.Status = MessageOperationStatus.Canceled;
                operation.StatusMessage = "Операція скасована";
                operation.CancelReason = reason;
                operation.CompletedAt = DateTime.UtcNow;
                operation.LastUpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Операція {CorrelationId} скасована: {Reason}", correlationId, reason);

                // Публікуємо подію про скасування операції
                await _bus.Publish(new MessageOperationCanceledEvent
                {
                    CorrelationId = correlationId,
                    Reason = reason
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при скасуванні операції {CorrelationId}", correlationId);
                throw;
            }
        }

        public async Task CompensateOperationAsync(Guid correlationId, string reason)
        {
            try
            {
                var operation = await GetOperationRequiredAsync(correlationId);

                operation.Status = MessageOperationStatus.Compensated;
                operation.StatusMessage = "Операція компенсована";
                operation.CancelReason = reason;
                operation.CompletedAt = DateTime.UtcNow;
                operation.LastUpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Операція {CorrelationId} компенсована: {Reason}", correlationId, reason);

                // Публікуємо подію про компенсацію операції
                await _bus.Publish(new MessageOperationCompensatedEvent
                {
                    CorrelationId = correlationId,
                    Reason = reason
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при компенсації операції {CorrelationId}", correlationId);
                throw;
            }
        }

        // Метод для отримання інформації про операцію
        public async Task<MessageOperation?> GetOperationAsync(Guid correlationId)
        {
            return await _context.MessageOperations
                .FirstOrDefaultAsync(op => op.CorrelationId == correlationId);
        }

        // Отримує активні операції користувача
        public async Task<IEnumerable<MessageOperation>> GetActiveOperationsForUserAsync(int userId)
        {
            return await _context.MessageOperations
                .Where(op => op.UserId == userId && op.IsActive)
                .OrderByDescending(op => op.CreatedAt)
                .ToListAsync();
        }

        // Отримує історію операцій користувача
        public async Task<IEnumerable<MessageOperation>> GetOperationHistoryForUserAsync(
            int userId, int pageNumber = 1, int pageSize = 20)
        {
            return await _context.MessageOperations
                .Where(op => op.UserId == userId)
                .OrderByDescending(op => op.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        // Отримує кількість операцій користувача
        public async Task<int> GetOperationCountForUserAsync(int userId)
        {
            return await _context.MessageOperations
                .CountAsync(op => op.UserId == userId);
        }

        // Отримує операції для конкретного чату
        public async Task<IEnumerable<MessageOperation>> GetOperationsForChatAsync(int chatRoomId)
        {
            return await _context.MessageOperations
                .Where(op => op.ChatRoomId == chatRoomId)
                .OrderByDescending(op => op.CreatedAt)
                .ToListAsync();
        }

        // Отримує операції для конкретного повідомлення
        public async Task<IEnumerable<MessageOperation>> GetOperationsForMessageAsync(int messageId)
        {
            return await _context.MessageOperations
                .Where(op => op.MessageId == messageId)
                .OrderByDescending(op => op.CreatedAt)
                .ToListAsync();
        }

        // Перевіряє, чи активна операція
        public async Task<bool> IsOperationActiveAsync(Guid correlationId)
        {
            var operation = await GetOperationAsync(correlationId);
            return operation?.IsActive ?? false;
        }

        // Перевіряє, чи можна скасувати операцію
        public async Task<bool> CanCancelOperationAsync(Guid correlationId)
        {
            var operation = await GetOperationAsync(correlationId);
            return operation?.CanBeCancelled ?? false;
        }

        // Перевіряє, чи виконується операція для повідомлення
        public async Task<bool> IsOperationInProgressAsync(int messageId, MessageOperationType operationType)
        {
            return await _context.MessageOperations
                .AnyAsync(op => op.MessageId == messageId &&
                               op.OperationType == operationType &&
                               op.IsActive);
        }

        // Очікує завершення операції
        public async Task<MessageOperation> WaitForOperationCompletionAsync(Guid correlationId, int timeoutSeconds = 30)
        {
            var startTime = DateTime.UtcNow;
            var timeoutTime = startTime.AddSeconds(timeoutSeconds);

            while (DateTime.UtcNow < timeoutTime)
            {
                var operation = await GetOperationAsync(correlationId);

                if (operation == null)
                {
                    await Task.Delay(500);
                    continue;
                }

                if (operation.IsCompleted)
                {
                    return operation;
                }

                await Task.Delay(500);
            }

            throw new TimeoutException($"Очікування завершення операції {correlationId} перевищило ліміт {timeoutSeconds} секунд");
        }
    }
}
