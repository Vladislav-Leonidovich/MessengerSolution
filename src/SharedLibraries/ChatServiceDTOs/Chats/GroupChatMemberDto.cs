using ChatService.Models;

namespace ChatService.DTOs
{
    public class GroupChatMemberDto
    {
        public int UserId { get; set; }
        public GroupRole Role { get; set; }
    }
}
