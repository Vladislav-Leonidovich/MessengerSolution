using Shared.DTOs.Message;

namespace MessageService.Sagas.MessageOperation.Events
{
    // Подія початку операції
    public class MessageOperationStartedEvent
    {
        public Guid CorrelationId { get; set; }
        public MessageOperationType OperationType { get; set; }
        public int? MessageId { get; set; }
        public int? ChatRoomId { get; set; }
        public int UserId { get; set; }
        public string? OperationData { get; set; }
    }

    // Подія оновлення прогресу операції
    public class MessageOperationProgressEvent
    {
        public Guid CorrelationId { get; set; }
        public int Progress { get; set; }
        public string StatusMessage { get; set; } = string.Empty;
    }

    // Подія успішного завершення операції
    public class MessageOperationCompletedEvent
    {
        public Guid CorrelationId { get; set; }
        public string? Result { get; set; }
    }

    // Подія невдалої операції
    public class MessageOperationFailedEvent
    {
        public Guid CorrelationId { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string? ErrorCode { get; set; }
    }

    // Подія скасування операції
    public class MessageOperationCanceledEvent
    {
        public Guid CorrelationId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    // Подія компенсації операції
    public class MessageOperationCompensatedEvent
    {
        public Guid CorrelationId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class MessageOperationStartCommand
    {
        public Guid CorrelationId { get; set; }
        public MessageOperationType OperationType { get; set; }
        public int? MessageId { get; set; }
        public int? ChatRoomId { get; set; }
        public int UserId { get; set; }
        public string? OperationData { get; set; } = null;
    }

    public class MessageOperationProgressCommand
    {
        public Guid CorrelationId { get; set; }
        public int Progress { get; set; }
        public string StatusMessage { get; set; } = string.Empty;
    }

    public class MessageOperationCompleteCommand
    {
        public Guid CorrelationId { get; set; }
        public string? Result { get; set; } = null;
    }

    public class MessageOperationFailCommand
    {
        public Guid CorrelationId { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string? ErrorCode { get; set; } = null;
    }

    public class MessageOperationCancelCommand
    {
        public Guid CorrelationId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class MessageOperationCompensateCommand
    {
        public Guid CorrelationId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
