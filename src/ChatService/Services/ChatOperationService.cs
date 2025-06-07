using System.Text.Json;
using ChatService.Data;
using ChatService.Models;
using ChatService.Services.Interfaces;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using ChatService.Sagas.ChatOperation.Events;
using Shared.DTOs.Chat;
using Shared.Exceptions;

namespace ChatService.Services
{
    public class ChatOperationService : IChatOperationService
    {
        private readonly ChatDbContext _context;
        private readonly IBus _bus;
        private readonly ILogger<ChatOperationService> _logger;

        public ChatOperationService(
            ChatDbContext context,
            IBus bus,
            ILogger<ChatOperationService> logger)
        {
            _context = context;
            _bus = bus;
            _logger = logger;
        }

        // === Основні методи управління життєвим циклом операції ===

        public async Task<ChatOperation> StartOperationAsync(
            Guid correlationId,
            ChatOperationType operationType,
            int chatRoomId,
            int userId,
            string? operationData = null)
        {
            try
            {
                // Перевіряємо, чи не існує вже операція з таким CorrelationId
                var existingOperation = await _context.ChatOperations
                    .FirstOrDefaultAsync(op => op.CorrelationId == correlationId);

                if (existingOperation != null)
                {
                    _logger.LogInformation("Операція з CorrelationId {CorrelationId} вже існує. Повертаємо існуючу.",
                        correlationId);
                    return existingOperation;
                }

                // Перевіряємо, чи немає конфліктних операцій
                await ValidateOperationAsync(operationType, chatRoomId, userId);

                var operation = new ChatOperation
                {
                    CorrelationId = correlationId,
                    OperationType = operationType,
                    ChatRoomId = chatRoomId,
                    UserId = userId,
                    Status = ChatOperationStatus.Pending,
                    OperationData = operationData,
                    StatusMessage = "Операція створена",
                    CreatedAt = DateTime.UtcNow,
                    LastUpdatedAt = DateTime.UtcNow
                };

                _context.ChatOperations.Add(operation);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Створено операцію {OperationType} для чату {ChatRoomId} користувачем {UserId}. CorrelationId: {CorrelationId}",
                    operationType, chatRoomId, userId, correlationId);

                // Публікуємо подію про початок операції
                await _bus.Publish(new ChatOperationStartedEvent
                {
                    CorrelationId = correlationId,
                    OperationId = correlationId,
                    OperationType = operationType,
                    ChatRoomId = chatRoomId,
                    UserId = userId,
                    OperationData = operationData
                });

                return operation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при створенні операції {OperationType} для чату {ChatRoomId}",
                    operationType, chatRoomId);
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
                if (operation.Status == ChatOperationStatus.Pending)
                {
                    operation.Status = ChatOperationStatus.InProgress;
                    operation.StartedAt = DateTime.UtcNow;
                }

                operation.Progress = progress;
                operation.StatusMessage = statusMessage;
                operation.LastUpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Оновлено прогрес операції {CorrelationId}: {Progress}% - {StatusMessage}",
                    correlationId, progress, statusMessage);

                // Публікуємо подію про оновлення прогресу
                await _bus.Publish(new ChatOperationProgressEvent
                {
                    CorrelationId = correlationId,
                    OperationId = correlationId,
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

                // Перевіряємо, чи можемо завершити операцію
                if (operation.IsCompleted)
                {
                    _logger.LogWarning("Спроба завершити вже завершену операцію {CorrelationId}", correlationId);
                    return;
                }

                operation.Status = ChatOperationStatus.Completed;
                operation.Progress = 100;
                operation.StatusMessage = "Операція успішно завершена";
                operation.Result = result;
                operation.CompletedAt = DateTime.UtcNow;
                operation.LastUpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Операція {CorrelationId} успішно завершена", correlationId);

                // Публікуємо подію про завершення операції
                await _bus.Publish(new ChatOperationCompletedEvent
                {
                    CorrelationId = correlationId,
                    OperationId = correlationId,
                    Result = result ?? string.Empty
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

                // Перевіряємо, чи можемо завершити операцію з помилкою
                if (operation.IsCompleted)
                {
                    _logger.LogWarning("Спроба завершити з помилкою вже завершену операцію {CorrelationId}", correlationId);
                    return;
                }

                operation.Status = ChatOperationStatus.Failed;
                operation.StatusMessage = "Операція завершена з помилкою";
                operation.ErrorMessage = errorMessage;
                operation.ErrorCode = errorCode;
                operation.CompletedAt = DateTime.UtcNow;
                operation.LastUpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogError("Операція {CorrelationId} завершена з помилкою: {ErrorMessage}",
                    correlationId, errorMessage);

                // Публікуємо подію про помилку операції
                await _bus.Publish(new ChatOperationFailedEvent
                {
                    CorrelationId = correlationId,
                    OperationId = correlationId,
                    ErrorMessage = errorMessage,
                    ErrorCode = errorCode ?? string.Empty
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при завершенні операції {CorrelationId} з помилкою", correlationId);
                throw;
            }
        }

        public async Task CancelOperationAsync(Guid correlationId, string reason)
        {
            try
            {
                var operation = await GetOperationRequiredAsync(correlationId);

                // Перевіряємо, чи можемо скасувати операцію
                if (!operation.CanBeCancelled)
                {
                    throw new InvalidOperationException($"Операція {correlationId} не може бути скасована. Поточний статус: {operation.Status}");
                }

                operation.Status = ChatOperationStatus.Failed; // Скасована операція = невдала операція
                operation.StatusMessage = "Операція скасована";
                operation.CancelReason = reason;
                operation.CompletedAt = DateTime.UtcNow;
                operation.LastUpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Операція {CorrelationId} скасована: {Reason}", correlationId, reason);

                // Публікуємо подію про скасування операції
                await _bus.Publish(new ChatOperationCancelledEvent
                {
                    CorrelationId = correlationId,
                    OperationId = correlationId,
                    CancelReason = reason
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

                operation.Status = ChatOperationStatus.Compensated;
                operation.StatusMessage = "Операція компенсована";
                operation.CancelReason = reason;
                operation.CompletedAt = DateTime.UtcNow;
                operation.LastUpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Операція {CorrelationId} компенсована: {Reason}", correlationId, reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при компенсації операції {CorrelationId}", correlationId);
                throw;
            }
        }

        // === Методи для отримання даних ===

        public async Task<ChatOperation?> GetOperationAsync(Guid correlationId)
        {
            return await _context.ChatOperations
                .FirstOrDefaultAsync(op => op.CorrelationId == correlationId);
        }

        public async Task<IEnumerable<ChatOperation>> GetActiveOperationsForUserAsync(int userId)
        {
            return await _context.ChatOperations
                .Where(op => op.UserId == userId && op.IsActive)
                .OrderByDescending(op => op.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<ChatOperation>> GetOperationHistoryForUserAsync(
            int userId, int pageNumber = 1, int pageSize = 20)
        {
            return await _context.ChatOperations
                .Where(op => op.UserId == userId)
                .OrderByDescending(op => op.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetOperationCountForUserAsync(int userId)
        {
            return await _context.ChatOperations
                .CountAsync(op => op.UserId == userId);
        }

        public async Task<IEnumerable<ChatOperation>> GetOperationsForChatAsync(int chatRoomId)
        {
            return await _context.ChatOperations
                .Where(op => op.ChatRoomId == chatRoomId)
                .OrderByDescending(op => op.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<ChatOperation>> GetActiveOperationsForChatAsync(int chatRoomId)
        {
            return await _context.ChatOperations
                .Where(op => op.ChatRoomId == chatRoomId && op.IsActive)
                .OrderByDescending(op => op.CreatedAt)
                .ToListAsync();
        }

        // Метод для вилучення ID чату з результату операції
        public int ExtractChatRoomIdFromResult(string? operationResult)
        {
            if (string.IsNullOrEmpty(operationResult))
            {
                throw new InvalidOperationException("Результат операції порожній");
            }

            try
            {
                // Розпаковуємо JSON з результату операції
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(operationResult);

                if (resultObj != null && resultObj.TryGetValue("ChatRoomId", out var chatRoomIdObj))
                {
                    // Конвертуємо значення в int
                    if (chatRoomIdObj is JsonElement element && element.ValueKind == JsonValueKind.Number)
                    {
                        return element.GetInt32();
                    }
                }

                throw new InvalidOperationException("Не вдалося отримати ID чату з результату операції");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при розборі результату операції: {OperationResult}", operationResult);
                throw new InvalidOperationException("Помилка при розборі результату операції", ex);
            }
        }

        // === Методи для перевірки стану ===

        public async Task<bool> IsOperationActiveAsync(Guid correlationId)
        {
            var operation = await GetOperationAsync(correlationId);
            return operation?.IsActive ?? false;
        }

        public async Task<bool> CanCancelOperationAsync(Guid correlationId)
        {
            var operation = await GetOperationAsync(correlationId);
            return operation?.CanBeCancelled ?? false;
        }

        public async Task<bool> IsOperationInProgressAsync(int chatRoomId, ChatOperationType operationType)
        {
            return await _context.ChatOperations
                .AnyAsync(op => op.ChatRoomId == chatRoomId &&
                               op.OperationType == operationType &&
                               op.IsActive);
        }

        public async Task<ChatOperation> WaitForOperationCompletionAsync(Guid correlationId, int timeoutSeconds = 30)
        {
            var startTime = DateTime.UtcNow;
            var timeoutTime = startTime.AddSeconds(timeoutSeconds);

            // Отримуємо інформацію про операцію
            var operation = await GetOperationAsync(correlationId);
            if (operation == null)
            {
                _logger.LogWarning("Операція з CorrelationId {CorrelationId} не знайдена", correlationId);
                throw new EntityNotFoundException("ChatOperation", correlationId);
            }

            while (operation.IsActive && DateTime.UtcNow < timeoutTime)
            {
                // Якщо операція завершена (успішно чи з помилкою), повертаємо результат
                if (operation.IsCompleted)
                {
                    _logger.LogInformation("Операція {CorrelationId} завершена зі статусом {Status}",
                        correlationId, operation.Status);
                    return operation;
                }

                // Чекаємо перед наступною перевіркою
                await Task.Delay(100);
            }

            // Якщо операція не завершилася за відведений час
            _logger.LogWarning("Час очікування операції {CorrelationId} минув", correlationId);
            throw new TimeoutException($"Час очікування операції {correlationId} минув");
        }

        // === Приватні допоміжні методи ===

        // Отримує операцію або викидає виняток, якщо не знайдено
        private async Task<ChatOperation> GetOperationRequiredAsync(Guid correlationId)
        {
            var operation = await GetOperationAsync(correlationId);
            if (operation == null)
            {
                throw new EntityNotFoundException("ChatOperation", correlationId);
            }
            return operation;
        }

        // Валідує можливість створення операції
        private async Task ValidateOperationAsync(ChatOperationType operationType, int chatRoomId, int userId)
        {
            // Перевіряємо конфліктні операції
            var conflictingOperations = await GetActiveOperationsForChatAsync(chatRoomId);

            foreach (var existingOp in conflictingOperations)
            {
                // Деякі операції не можуть виконуватись одночасно
                if (AreOperationsConflicting(operationType, existingOp.OperationType))
                {
                    throw new InvalidOperationException(
                        $"Не можна виконати операцію {operationType} для чату {chatRoomId}, " +
                        $"оскільки вже виконується операція {existingOp.OperationType}");
                }
            }

            // Додаткові перевірки можна додати тут
        }

        // Перевіряє, чи конфліктують операції між собою
        private static bool AreOperationsConflicting(ChatOperationType newOp, ChatOperationType existingOp)
        {
            // Правила конфліктів операцій
            return (newOp, existingOp) switch
            {
                // Видалення конфліктує з усіма іншими операціями
                (ChatOperationType.Delete, _) => true,
                (_, ChatOperationType.Delete) => true,

                // Архівування конфліктує з більшістю операцій
                (ChatOperationType.Archive, _) when existingOp != ChatOperationType.Archive => true,
                (_, ChatOperationType.Archive) when newOp != ChatOperationType.Archive => true,

                // Зміна власника конфліктує з додаванням/видаленням учасників
                (ChatOperationType.ChangeOwner, ChatOperationType.AddMember) => true,
                (ChatOperationType.ChangeOwner, ChatOperationType.RemoveMember) => true,
                (ChatOperationType.AddMember, ChatOperationType.ChangeOwner) => true,
                (ChatOperationType.RemoveMember, ChatOperationType.ChangeOwner) => true,

                // За замовчуванням операції не конфліктують
                _ => false
            };
        }
    }
}
