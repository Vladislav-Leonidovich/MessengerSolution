using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageService.Sagas.DeleteAllMessages.Events
{
    public class DeleteAllChatMessagesCommand
    {
        public Guid CorrelationId { get; set; } = Guid.NewGuid();
        public int ChatRoomId { get; set; }
        public int InitiatedByUserId { get; set; }
    }

    public class DeleteChatMessagesCommand
    {
        public Guid CorrelationId { get; set; }
        public int ChatRoomId { get; set; }
        public int InitiatedByUserId { get; set; }
    }

    public class MessagesDeletedEvent
    {
        public Guid CorrelationId { get; set; }
        public int ChatRoomId { get; set; }
        public int MessageCount { get; set; }
        public DateTime DeletedAt { get; set; } = DateTime.UtcNow;
    }

    public class ErrorEvent
    {
        public Guid CorrelationId { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class NotificationsSentEvent
    {
        public Guid CorrelationId { get; set; }
        public int RecipientCount { get; set; }
    }

    public class SendChatNotificationCommand
    {
        public Guid CorrelationId { get; set; }
        public int ChatRoomId { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class DeleteMessagesSagaTimeoutEvent
    {
        public Guid CorrelationId { get; set; }
        public string TimeoutReason { get; set; } = string.Empty;
    }

    public class CompensateMessagesDeleteCommand
    {
        public Guid CorrelationId { get; set; }
        public int ChatRoomId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
