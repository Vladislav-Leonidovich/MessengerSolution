using MessageServiceDTOs;

namespace ChatServiceDTOs.Chats
{
    public class GroupChatRoomDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public int OwnerId { get; set; }
        public MessageDto LastMessagePreview { get; set; } = new MessageDto();
        public IEnumerable<GroupChatMemberDto> Members { get; set; } = new List<GroupChatMemberDto>();
        public ChatRoomType ChatRoomType { get; set; }
        public int? FolderId { get; set; } = null;
    }
}
