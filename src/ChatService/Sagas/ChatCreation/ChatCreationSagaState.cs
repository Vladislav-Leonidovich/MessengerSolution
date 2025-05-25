using MassTransit;

namespace ChatService.Sagas.ChatCreation
{
    public class ChatCreationSagaState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public string CurrentState { get; set; } = string.Empty;
        public int ChatRoomId { get; set; }
        public int CreatorUserId { get; set; }
        public string MemberIdsJson { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public Guid? TimeoutTokenId { get; set; }
        public int Progress { get; set; } = 0;
        public string StatusMessage { get; set; } = "Очікування початку створення чату";
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsCompleted { get; set; } = false;
        public bool IsCompensated { get; set; } = false;
        public int RetryCount { get; set; } = 0;
        public string? AdditionalData { get; set; }
        public bool IsActive => !IsCompleted && !IsCompensated;
        public TimeSpan? Duration => CompletedAt.HasValue && StartedAt.HasValue
            ? CompletedAt.Value - StartedAt.Value
            : null;
    }
}
