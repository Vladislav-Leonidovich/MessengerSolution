using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Contracts
{
    public abstract class ChatEventBase
    {
        public Guid EventId { get; set; } = Guid.NewGuid();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class ChatDeletedEvent
    {
        public int ChatRoomId { get; set; }
        public int UserId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class ChatAccessChangedEvent
    {
        public int ChatRoomId { get; set; }
        public int UserId { get; set; }
        public bool HasAccess { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
