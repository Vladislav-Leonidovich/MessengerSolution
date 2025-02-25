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

        public async Task<IEnumerable<FolderDto>> GetFoldersAsync(int userId)
        {
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<FolderDto>>($"api/folder/user/{userId}");
            return response ?? new List<FolderDto>();
        }
    }
}
