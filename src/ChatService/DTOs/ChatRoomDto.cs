namespace ChatService.DTOs
{
    // DTO для повернення даних про чат
    public class ChatRoomDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}
