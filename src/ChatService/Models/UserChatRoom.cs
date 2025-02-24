namespace ChatService.Models
{
    public class UserChatRoom
    {
        // Ідентифікатор чату
        public int PrivateChatRoomId { get; set; }
        public PrivateChatRoom PrivateChatRoom { get; set; } = null!;
        // Ідентифікатор користувача (повинен відповідати ID з IdentityService)
        public int UserId { get; set; }
    }
}
