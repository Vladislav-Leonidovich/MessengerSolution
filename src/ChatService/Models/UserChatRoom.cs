namespace ChatService.Models
{
    public class UserChatRoom
    {
        // Ідентифікатор чату
        public int ChatRoomId { get; set; }
        public ChatRoom ChatRoom { get; set; } = null!;
        // Ідентифікатор користувача (повинен відповідати ID з IdentityService)
        public int UserId { get; set; }
    }
}
