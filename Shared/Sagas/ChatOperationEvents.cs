using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.DTOs.Chat;

namespace Shared.Sagas
{
    // Базовий клас для всіх подій операцій
    public abstract class ChatOperationEventBase
    {
        public Guid CorrelationId { get; set; }
        public Guid OperationId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    // Подія початку операції
    public class ChatOperationStartedEvent : ChatOperationEventBase
    {
        public ChatOperationType OperationType { get; set; }
        public int ChatRoomId { get; set; }
        public int UserId { get; set; }
        public string? OperationData { get; set; }
    }

    // Подія оновлення прогресу операції
    public class ChatOperationProgressEvent : ChatOperationEventBase
    {
        public int Progress { get; set; }
        public string StatusMessage { get; set; } = string.Empty;
    }

    // Подія завершення операції
    public class ChatOperationCompletedEvent : ChatOperationEventBase
    {
        public string Result { get; set; } = string.Empty;
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    }

    // Подія помилки операції
    public class ChatOperationFailedEvent : ChatOperationEventBase
    {
        public string ErrorMessage { get; set; } = string.Empty;
        public string ErrorCode { get; set; } = string.Empty;
        public DateTime FailedAt { get; set; } = DateTime.UtcNow;
    }

    // Подія скасування операції
    public class ChatOperationCancelledEvent : ChatOperationEventBase
    {
        public string CancelReason { get; set; } = string.Empty;
        public DateTime CancelledAt { get; set; } = DateTime.UtcNow;
    }
}
