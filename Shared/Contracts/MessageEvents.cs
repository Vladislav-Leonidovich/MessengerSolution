using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.DTOs.Chat;

namespace Shared.Contracts
{
    public abstract class MessageEventBase
    {
        public Guid EventId { get; set; } = Guid.NewGuid();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    // Контракт події, який публікується при створенні нового повідомлення
    public class MessageCreatedEvent : MessageEventBase
    {
        public int Id { get; set; }
        public int ChatRoomId { get; set; }
        public ChatRoomType ChatRoomType { get; set; }
        public int SenderUserId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public bool IsEdited { get; set; }
        public DateTime? EditedAt { get; set; }
    }

    public class MessageUpdatedEvent : MessageEventBase
    {
        public int Id { get; set; }
        public int ChatRoomId { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public bool IsEdited { get; set; }
        public DateTime? EditedAt { get; set; }
    }

    public class MessageDeletedEvent : MessageEventBase
    {
        public int Id { get; set; }
        public int ChatRoomId { get; set; }
        public int DeletedByUserId { get; set; }
        public DateTime DeletedAt { get; set; }
    }
}
