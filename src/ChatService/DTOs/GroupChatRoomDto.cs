namespace ChatService.DTOs
{
    public class GroupChatRoomDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public int OwnerId { get; set; }
        public IEnumerable<GroupChatMemberDto> Members { get; set; } = new List<GroupChatMemberDto>();
    }
}
