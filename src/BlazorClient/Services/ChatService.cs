using System.Net.Http.Json;

namespace BlazorClient.Services
{
    // Реалізація сервісу роботи з чатами
    public class ChatService : IChatService
    {
        private readonly HttpClient _httpClient;

        // Конструктор з впровадженням HttpClient
        public ChatService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Отримання списку чатів для користувача
        public async Task<IEnumerable<ChatRoomResponse>> GetChatRoomsAsync(int userId)
        {
            // Використовуємо GET запит до endpoint'у для отримання чатів, передаючи userId як параметр
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<ChatRoomResponse>>($"api/chat/user/{userId}");
            return response ?? new List<ChatRoomResponse>();
        }

        // Створення нового чату
        public async Task<ChatRoomResponse?> CreateChatRoomAsync(CreateChatRoomModel model)
        {
            // Надсилаємо POST запит на endpoint створення чату
            var response = await _httpClient.PostAsJsonAsync("api/chat/create", model);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ChatRoomResponse>();
            }
            return null;
        }
    }
}
