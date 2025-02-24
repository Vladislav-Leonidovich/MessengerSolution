using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using MauiClient.Models.Chat;

namespace MauiClient.Services
{
    // Реалізація сервісу роботи з чатами
    public class ChatService : IChatService
    {
        private readonly System.Net.Http.HttpClient _httpClient;

        public ChatService(System.Net.Http.HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<ChatRoomDto>> GetChatRoomsAsync(int userId)
        {
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<ChatRoomDto>>($"api/chat/user/{userId}");
            return response ?? new List<ChatRoomDto>();
        }

        public async Task<ChatRoomDto?> CreateChatRoomAsync(CreateChatRoomDto model)
        {
            var response = await _httpClient.PostAsJsonAsync("api/chat/create", model);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ChatRoomDto>();
            }
            return null;
        }
    }
}
