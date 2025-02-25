using System;

namespace ChatService.DTOs
{
    // DTO для повернення даних про чат
    public class ChatRoomDto
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
