using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Contracts
{
    // Контракт події, який публікується при створенні нового повідомлення
    public class MessageCreatedEvent
    {
        public int MessageId { get; set; }
        public int ChatRoomId { get; set; }
        public int SenderUserId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
