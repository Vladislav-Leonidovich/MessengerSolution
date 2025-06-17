using IdentityService.Services.Interfaces;

namespace IdentityService.Services
{
    public class TokenService : ITokenService
    {
        private readonly ILogger<TokenService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAuthService _authService;
        private string _cachedServiceToken;
        private DateTime _tokenExpiration = DateTime.MinValue;

        public TokenService(
            IHttpContextAccessor httpContextAccessor,
            IAuthService authService,
            ILogger<TokenService> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _authService = authService;
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

            return Task.FromResult(string.Empty);
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



                var response = await _authService.GenerateServiceTokenAsync("IdentityService");
                if (response != null && !string.IsNullOrEmpty(response))
                {
                    // Кешуємо токен на годину менше ніж його термін дії (зазвичай 12 годин)
                    _cachedServiceToken = response;
                    _tokenExpiration = DateTime.UtcNow.AddHours(11);
                    return _cachedServiceToken;
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
