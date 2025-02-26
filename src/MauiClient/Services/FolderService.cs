using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using ChatServiceDTOs.Folders;

namespace MauiClient.Services
{
    public class FolderService : IFolderService
    {
        private readonly HttpClient _httpClient;

        public FolderService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<FolderDto>> GetFoldersAsync()
        {
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<FolderDto>>($"api/folder");
            return response ?? new List<FolderDto>();
        }

        public async Task<FolderDto?> CreateFolderAsync(CreateFolderDto model)
        {
            var response = await _httpClient.PostAsJsonAsync("api/folder/create", model);
            if(response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<FolderDto>();
            }
            return null;
        }

        public async Task<FolderDto?> UpdateFolderAsync(FolderDto folder)
        {
            var response = await _httpClient.PutAsJsonAsync("api/folder/update", folder);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<FolderDto>();
            }
            return null;
        }

        public async Task<bool> DeleteFolderAsync(int folderId)
        {
            var response = await _httpClient.DeleteAsync($"api/folder/delete{folderId}");
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> AssignPrivateChatToFolderAsync(int folderId, int chatId)
        {
            var response = await _httpClient.PostAsync($"api/folder/{folderId}/assign-private-chat/{chatId}", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UnassignPrivateChatFromFolderAsync(int folderId, int chatId)
        {
            var response = await _httpClient.PostAsync($"api/folder/{folderId}/unassign-private-chat/{chatId}", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> AssignGroupChatToFolderAsync(int folderId, int chatId)
        {
            var response = await _httpClient.PostAsync($"api/folder/{folderId}/assign-group-chat/{chatId}", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UnassignGroupChatFromFolderAsync(int folderId, int chatId)
        {
            var response = await _httpClient.PostAsync($"api/folder/{folderId}/unassign-group-chat/{chatId}", null);
            return response.IsSuccessStatusCode;
        }
    }
}
