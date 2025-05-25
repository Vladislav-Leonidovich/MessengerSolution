using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.ChatServiceDTOs.Chats;

namespace Shared.Sagas
{
    public class CreateChatRoomCommand
    {
        public Guid CorrelationId { get; set; } = Guid.NewGuid();
        public int ChatRoomId { get; set; }
        public int CreatorUserId { get; set; }
        public List<int> MemberIds { get; set; } = new List<int>();
    }

    public class NotifyMessageServiceCommand
    {
        public Guid CorrelationId { get; set; }
        public int ChatRoomId { get; set; }
    }

    public class CompensateChatCreationCommand
    {
        public Guid CorrelationId { get; set; }
        public int ChatRoomId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class ChatCreationStartedEvent
    {
        public Guid CorrelationId { get; set; } = Guid.NewGuid();
        public int ChatRoomId { get; set; }
        public int CreatorUserId { get; set; }
        public List<int> MemberIds { get; set; } = new List<int>();
    }

    public class ChatRoomCreatedEvent
    {
        public Guid CorrelationId { get; set; }
        public int ChatRoomId { get; set; }
    }

    public class MessageServiceNotifiedEvent
    {
        public Guid CorrelationId { get; set; }
        public int ChatRoomId { get; set; }
    }

    public class CompleteChatCreationCommand
    {
        public Guid CorrelationId { get; set; }
        public int ChatRoomId { get; set; }
    }

    public class ChatCreationFailedEvent
    {
        public Guid CorrelationId { get; set; }
        public int ChatRoomId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class ChatCreationCompensatedEvent
    {
        public Guid CorrelationId { get; set; }
        public int ChatRoomId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class ChatCreationSagaTimeoutEvent
    {
        public Guid CorrelationId { get; set; }
        public string TimeoutReason { get; set; } = string.Empty;
    }

    public class ChatOperationStartCommand
    {
        public Guid CorrelationId { get; set; }
        public ChatOperationType OperationType { get; set; }
        public int ChatRoomId { get; set; }
        public int UserId { get; set; }
        public string? OperationData { get; set; }
    }

    public class ChatOperationProgressCommand
    {
        public Guid CorrelationId { get; set; }
        public int Progress { get; set; }
        public string StatusMessage { get; set; } = string.Empty;
    }

    public class ChatOperationCompleteCommand
    {
        public Guid CorrelationId { get; set; }
        public string? Result { get; set; }
    }

    public class ChatOperationFailCommand
    {
        public Guid CorrelationId { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string? ErrorCode { get; set; }
    }

    public class ChatOperationCompensateCommand
    {
        public Guid CorrelationId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
