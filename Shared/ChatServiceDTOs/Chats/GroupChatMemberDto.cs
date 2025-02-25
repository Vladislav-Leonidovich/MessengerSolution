using ChatServiceModels.Chats;

namespace ChatServiceDTOs.Chats
{
    public class GroupChatMemberDto
    {
        public int UserId { get; set; }
        public GroupRole Role { get; set; }
    }
}
