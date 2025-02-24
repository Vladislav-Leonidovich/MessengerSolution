namespace ChatService.DTOs
{
    public class CreateFolderDto
    {
        // Назва папки
        public string Name { get; set; } = null!;
        public int Order { get; set; }
        public List<int>? ChatRoomIds { get; set; }

    }
}
