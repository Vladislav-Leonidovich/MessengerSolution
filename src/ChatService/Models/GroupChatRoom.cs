namespace ChatService.Models
{
    // Груповий чат із додатковими властивостями
    public class GroupChatRoom : ChatRoom
    {
        // Назва групового чату, яку встановлює власник
        public string Name { get; set; } = null!;
        // Ідентифікатор власника групи
        public int OwnerId { get; set; }
        // Колекція учасників групи із вказанням ролі
        public ICollection<GroupChatMember> GroupChatMembers { get; set; } = new List<GroupChatMember>();
    }
}
