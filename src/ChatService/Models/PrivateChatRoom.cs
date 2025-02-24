namespace ChatService.Models
{
    // Приватний чат між двома користувачами
    public class PrivateChatRoom : ChatRoom
    {
        // Для приватного чату використовується зв'язкова таблиця для двох користувачів
        public ICollection<UserChatRoom> UserChatRooms { get; set; } = new List<UserChatRoom>();
    }
}
