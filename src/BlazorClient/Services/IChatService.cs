namespace BlazorClient.Services
{
    // Інтерфейс для сервісу роботи з чатами
    public interface IChatService
    {
        // Метод для отримання списку чатів користувача
        Task<IEnumerable<ChatRoomResponse>> GetChatRoomsAsync(int userId);

        // Метод для створення нового чату
        Task<ChatRoomResponse?> CreateChatRoomAsync(CreateChatRoomModel model);
    }

    // DTO для створення чату
    public class CreateChatRoomModel
    {
        public string Name { get; set; } = string.Empty;
        public List<int> UserIds { get; set; } = new List<int>();
    }

    // DTO для відповіді при отриманні даних про чат
    public class ChatRoomResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<int> UserIds { get; set; } = new List<int>();
    }
}
