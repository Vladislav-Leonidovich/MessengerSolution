using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Shared.DTOs.Identity;
using Shared.DTOs.Message;
using Shared.DTOs.Responses;

namespace MauiClient.Services
{
    public class UserService : IUserService
    {
        private readonly HttpClient _httpClient;

        public UserService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<UserDto?> SearchUserByUsernameAsync(string username)
        {
            try
            {
                // Використовуємо правильний шлях API
                var httpResponse = await _httpClient.GetAsync($"api/users/search/username/{Uri.EscapeDataString(username)}");
                var jsonContent = await httpResponse.Content.ReadAsStringAsync();
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true // Це ключове налаштування!
                };
                if (httpResponse.IsSuccessStatusCode)
                {
                    var content = System.Text.Json.JsonSerializer.Deserialize<ApiResponse<UserDto>>(jsonContent, options);
                    return content?.Data;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при пошуку користувача: {ex.Message}");
                return null;
            }
        }
    }
}
