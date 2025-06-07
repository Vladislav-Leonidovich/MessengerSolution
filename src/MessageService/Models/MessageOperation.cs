using Shared.DTOs.Message;

namespace MessageService.Models
{
    public class MessageOperation
    {
        // Унікальний ідентифікатор операції (CorrelationId саги)
        public Guid CorrelationId { get; set; }

        // Тип операції
        public MessageOperationType OperationType { get; set; }

        // Поточний статус операції
        public MessageOperationStatus Status { get; set; } = MessageOperationStatus.Pending;

        // ID повідомлення, над яким виконується операція (може бути null для групових операцій)
        public int? MessageId { get; set; }

        // ID чату, якщо операція стосується всіх повідомлень чату
        public int? ChatRoomId { get; set; }

        // ID користувача, який ініціював операцію
        public int UserId { get; set; }

        // Прогрес виконання операції (0-100)
        public int Progress { get; set; } = 0;

        // Поточний статус операції (текстовий опис)
        public string StatusMessage { get; set; } = string.Empty;

        // Додаткові дані операції у форматі JSON
        public string? OperationData { get; set; }

        // Результат операції (якщо операція завершена успішно)
        public string? Result { get; set; }

        // Опис помилки (якщо операція завершилась невдало)
        public string? ErrorMessage { get; set; }

        // Код помилки (якщо операція завершилась невдало)
        public string? ErrorCode { get; set; }

        // Причина скасування або компенсації
        public string? CancelReason { get; set; }

        // Час створення операції
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Час початку виконання операції
        public DateTime? StartedAt { get; set; }

        // Час завершення операції (успішного або невдалого)
        public DateTime? CompletedAt { get; set; }

        // Час останнього оновлення операції
        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

        // Перевіряє, чи активна операція
        public bool IsActive => Status == MessageOperationStatus.Pending ||
                                Status == MessageOperationStatus.InProgress;

        // Перевіряє, чи завершена операція
        public bool IsCompleted => Status == MessageOperationStatus.Completed ||
                                  Status == MessageOperationStatus.Failed ||
                                  Status == MessageOperationStatus.Compensated;

        // Перевіряє, чи можна скасувати операцію
        public bool CanBeCancelled => Status == MessageOperationStatus.Pending ||
                                     Status == MessageOperationStatus.InProgress;

        // Тривалість операції
        public TimeSpan? Duration => CompletedAt.HasValue ?
                                    CompletedAt.Value - (StartedAt ?? CreatedAt) : null;
    }

    public enum MessageOperationStatus
    {
        Pending = 0,
        InProgress = 1,
        Completed = 2,
        Failed = 3,
        Canceled = 4,
        Compensated = 5
    }
}
