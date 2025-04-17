using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessageServiceDTOs;

namespace Shared.Contracts
{
    // Контракт події, який публікується при створенні нового повідомлення
    public class MessageCreatedEvent
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

    public class MessageUpdatedEvent
    {
        public int Id { get; set; }
        public int ChatRoomId { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public bool IsEdited { get; set; }
        public DateTime? EditedAt { get; set; }
    }
}
