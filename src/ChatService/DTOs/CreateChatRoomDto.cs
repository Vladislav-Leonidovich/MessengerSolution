namespace ChatService.DTOs
{
    // DTO для створення нового чату
    public class CreateChatRoomDto
    {
        // Назва чату
        public string Name { get; set; } = null!;
        // Список ID користувачів, які беруть участь у чаті
        public List<int> UserIds { get; set; } = new List<int>();
    }
}
