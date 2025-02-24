namespace ChatService.Models
{
    public class ChatRoom
    {
        public int Id { get; set; }
        // Назва чату
        public string Name { get; set; } = null!;
        // Дата створення чату
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        // Колекція користувачів, що беруть участь у чаті
        public ICollection<UserChatRoom> UserChatRooms { get; set; } = new List<UserChatRoom>();
        // Посилання на папку. Якщо null – чат не віднесений до жодної папки.
        public int? FolderId { get; set; }
        public Folder? Folder { get; set; }
    }
}
