using System.Net.Http.Json;

namespace BlazorClient.Services
{
    // Реалізація сервісу автентифікації
    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;

        // Конструктор з впровадженням HttpClient
        public AuthService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Реєстрація користувача
        public async Task<bool> RegisterAsync(RegisterModel model)
        {
            // Надсилаємо POST запит на endpoint реєстрації через Gateway
            var response = await _httpClient.PostAsJsonAsync("api/auth/register", model);
            return response.IsSuccessStatusCode;
        }

        // Вхід користувача
        public async Task<string?> LoginAsync(LoginModel model)
        {
            // Надсилаємо POST запит на endpoint логіну через Gateway
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", model);
            if (response.IsSuccessStatusCode)
            {
                // При успішному вході отримуємо токен з відповіді
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                return result?.Token;
            }
            return null;
        }
    }

    // DTO для відповіді при логіні
    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
}
