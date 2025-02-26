using System;

namespace ChatServiceDTOs.Chats
{
    // DTO для повернення даних про чат
    public class ChatRoomDto
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Name { get; set; } = string.Empty;
        // Список ідентифікаторів учасників
        public IEnumerable<int> ParticipantIds { get; set; } = new List<int>();
    }
}
