using Shared.ChatServiceDTOs.Chats;

namespace ChatService.Models
{
    public class ChatOperation
    {

        // Унікальний ідентифікатор операції (CorrelationId саги)

        public Guid CorrelationId { get; set; }


        // Тип операції (створення, видалення, оновлення тощо)

        public ChatOperationType OperationType { get; set; }


        // Поточний статус операції

        public ChatOperationStatus Status { get; set; } = ChatOperationStatus.Pending;


        // ID чату, над яким виконується операція

        public int ChatRoomId { get; set; }


        // ID користувача, який ініціював операцію

        public int UserId { get; set; }


        // Прогрес виконання операції (0-100)

        public int Progress { get; set; } = 0;


        // Поточний статус операції (текстовий опис)

        public string StatusMessage { get; set; } = string.Empty;


        // Додаткові дані операції у форматі JSON
        // Може містити параметри операції, проміжні результати тощо

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

        public bool IsActive => Status == ChatOperationStatus.Pending || Status == ChatOperationStatus.InProgress;


        // Перевіряє, чи завершена операція

        public bool IsCompleted => Status == ChatOperationStatus.Completed ||
                                  Status == ChatOperationStatus.Failed ||
                                  Status == ChatOperationStatus.Compensated;


        // Переевіряє, чи можна скасувати операцію

        public bool CanBeCancelled => Status == ChatOperationStatus.Pending || Status == ChatOperationStatus.InProgress;


        // Тривалість операції

        public TimeSpan? Duration => CompletedAt.HasValue ? CompletedAt.Value - CreatedAt : null;


        // Перевіряє, чи операція успішна

        public bool IsSuccessful => Status == ChatOperationStatus.Completed;

        // Перевіряє, чи операція невдала
        public bool IsFailed => Status == ChatOperationStatus.Failed;
    }

    // Enum для статусів операцій
    public enum ChatOperationStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed,
        Compensated,
        PartiallyCompleted
    }
}
