﻿namespace MessageServiceDTOs
{
    public class MessageDto
    {
        public int Id { get; set; }
        public int ChatRoomId { get; set; }
        public int SenderUserId { get; set; }
        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}
