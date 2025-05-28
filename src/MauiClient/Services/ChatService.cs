using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Shared.DTOs.Chat;

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

        public async Task<ChatRoomDto?> CreatePrivateChatRoomAsync(CreatePrivateChatRoomDto model)
        {
            var response = await _httpClient.PostAsJsonAsync("api/chat/create-private", model);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ChatRoomDto>();
            }
            return null;
        }

        public async Task<GroupChatRoomDto?> CreateGroupChatRoomAsync(CreateGroupChatRoomDto model)
        {
            var response = await _httpClient.PostAsJsonAsync("api/chat/create-group", model);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<GroupChatRoomDto>();
            }
            return null;
        }

        public async Task<IEnumerable<ChatRoomDto>> GetPrivateChatRoomsWithoutFolderAsync()
        {
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<ChatRoomDto>>("api/chat/private/no-folder");
            return response ?? new List<ChatRoomDto>();
        }

        public async Task<IEnumerable<GroupChatRoomDto>> GetGroupChatRoomsWithoutFolderAsync()
        {
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<GroupChatRoomDto>>("api/chat/group/no-folder");
            return response ?? new List<GroupChatRoomDto>();
        }

        public async Task<IEnumerable<ChatRoomDto>> GetPrivateChatRoomsForFolderAsync(int folderId)
        {
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<ChatRoomDto>>($"api/chat/private/{folderId}");
            return response ?? new List<ChatRoomDto>();
        }

        public async Task<IEnumerable<GroupChatRoomDto>> GetGroupChatRoomsForFolderAsync(int folderId)
        {
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<GroupChatRoomDto>>($"api/chat/group/{folderId}");
            return response ?? new List<GroupChatRoomDto>();
        }

        public async Task<IEnumerable<ChatRoomDto>> GetPrivateChatRoomsAsync()
        {
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<ChatRoomDto>>("api/chat/private");
            return response ?? new List<ChatRoomDto>();
        }

        public async Task<IEnumerable<GroupChatRoomDto>> GetGroupChatRoomsAsync()
        {
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<GroupChatRoomDto>>("api/chat/group");
            return response ?? new List<GroupChatRoomDto>();
        }

        public async Task<ChatRoomDto> GetPrivateChatRoomAsync(int chatId)
        {
            var response = await _httpClient.GetFromJsonAsync<ChatRoomDto>($"api/chat/private/{chatId}");
            return response ?? new ChatRoomDto();
        }

        public async Task<GroupChatRoomDto> GetGroupChatRoomAsync(int chatId)
        {
            var response = await _httpClient.GetFromJsonAsync<GroupChatRoomDto>($"api/chat/group/{chatId}");
            return response ?? new GroupChatRoomDto();
        }

        public async Task<bool> DeletePrivateChatRoomAsync(int chatId)
        {
            var response = await _httpClient.DeleteAsync($"api/chat/delete-private/{chatId}");
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteGroupChatRoomAsync(int chatId)
        {
            var response = await _httpClient.DeleteAsync($"api/chat/delete-group/{chatId}");
            return response.IsSuccessStatusCode;
        }
    }
}
