using EncryptionService.Services.Interfaces;

namespace EncryptionService.Services
{
    public class TokenService : ITokenService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TokenService> _logger;
        private string _cachedServiceToken;
        private DateTime _tokenExpiration = DateTime.MinValue;

        public TokenService(
            IHttpContextAccessor httpContextAccessor,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<TokenService> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;

        }

        public Task<string> GetTokenAsync()
        {
            // Отримуємо токен з поточного HTTP-контексту
            var authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                return Task.FromResult(authHeader.Substring("Bearer ".Length).Trim());
            }

            return Task.FromResult<string>(string.Empty);
        }

        public async Task<string> GetServiceToServiceTokenAsync()
        {
            try
            {
                // Перевіряємо кеш
                if (!string.IsNullOrEmpty(_cachedServiceToken) && DateTime.UtcNow < _tokenExpiration)
                {
                    return _cachedServiceToken;
                }

                // Спочатку перевіряємо, чи є токен в поточному запиті
                var userToken = await GetTokenAsync();
                if (!string.IsNullOrEmpty(userToken))
                {
                    return userToken; // Використовуємо токен користувача, якщо він є
                }

                // Отримуємо конфігурацію
                var identityServiceUrl = _configuration["ServiceTokens:IdentityServiceUrl"];
                var apiKey = _configuration["ServiceTokens:ApiKey"];
                var serviceName = _configuration["ServiceTokens:ServiceName"];

                if (string.IsNullOrEmpty(identityServiceUrl) || string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogWarning("Не налаштовано параметри для отримання service-to-service токена");
                    return string.Empty;
                }

                // Створюємо HTTP клієнт для запиту до Identity Service
                var client = _httpClientFactory.CreateClient("IdentityServiceClient");

                // Створюємо запит на отримання сервісного токена
                var request = new HttpRequestMessage(HttpMethod.Post, $"{identityServiceUrl}/api/internal/auth/token");
                request.Headers.Add("X-API-Key", apiKey);
                request.Content = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(new { ServiceName = serviceName }),
                    System.Text.Encoding.UTF8,
                    "application/json");

                var response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var tokenResponse = await response.Content.ReadFromJsonAsync<ServiceTokenResponse>();
                    if (tokenResponse != null && !string.IsNullOrEmpty(tokenResponse.Token))
                    {
                        // Кешуємо токен на годину менше ніж його термін дії (зазвичай 12 годин)
                        _cachedServiceToken = tokenResponse.Token;
                        _tokenExpiration = DateTime.UtcNow.AddHours(11);
                        return _cachedServiceToken;
                    }
                }
                else
                {
                    _logger.LogError("Помилка отримання сервісного токена. Статус: {StatusCode}", response.StatusCode);
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при отриманні service-to-service токена");
                return string.Empty;
            }
        }

        private class ServiceTokenResponse
        {
            public string Token { get; set; }
        }
    }
}
