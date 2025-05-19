namespace ChatService.Models
{
    // Модель для повідомлень в паттерні Outbox
    public class OutboxMessage
    {
        public Guid Id { get; set; }
        // Тип події (наприклад, "ChatCreated")
        public string EventType { get; set; } = string.Empty;
        // Серіалізовані дані події в JSON
        public string EventData { get; set; } = string.Empty;
        // Час створення повідомлення
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        // Час обробки повідомлення (null якщо не оброблено)
        public DateTime? ProcessedAt { get; set; }
        // Кількість спроб обробки
        public int RetryCount { get; set; } = 0;
        // Помилка, якщо була
        public string? Error { get; set; }
    }
}
