using Shared.DTOs.Common;

namespace Shared.DTOs.Message
{
    public class MessageDto
    {
        public int Id { get; set; }
        public int ChatRoomId { get; set; }
        public int SenderUserId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }
        public bool IsEdited { get; set; } = false;
        public DateTime? EditedAt { get; set; }
        public MessageStatus Status { get; set; }
    }
}
