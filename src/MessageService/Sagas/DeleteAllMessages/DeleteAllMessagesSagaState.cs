using MassTransit;

namespace MessageService.Sagas.DeleteAllMessages
{
    public class DeleteAllMessagesSagaState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public string CurrentState { get; set; } = string.Empty;
        public int ChatRoomId { get; set; }
        public int InitiatedByUserId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public int DeletedMessageCount { get; set; } // Кількість видалених повідомлень
        public List<int> NotifiedUserIds { get; set; } = new List<int>(); // Кому надіслано сповіщення

        // Токен для таймауту
        public Guid? TimeoutTokenId { get; set; }

        // Додаткова інформація для аудиту
        public DateTime? LastUpdatedAt { get; set; }
        public string LastError { get; set; } = string.Empty;
        public int RetryCount { get; set; } = 0;
    }
}
