using MessageServiceDTOs;

namespace ChatService.Models
{
    // Абстрактний базовий клас для чатів
    public abstract class ChatRoom
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        // Загальний зв’язок із папкою, якщо чат відноситься до неї
        public int? FolderId { get; set; }
        public Folder? Folder { get; set; }
        public ChatRoomType ChatRoomType { get; set; }
    }
}
