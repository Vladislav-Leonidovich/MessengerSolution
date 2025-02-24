namespace ChatService.Models
{
    public class GroupChatMember
    {
        public int GroupChatRoomId { get; set; }
        public GroupChatRoom GroupChatRoom { get; set; } = null!;
        public int UserId { get; set; }
        public GroupRole Role { get; set; }
    }
}
