using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
}
