using ChatService.Services.Interfaces;

namespace MessageService.Services
{
    public class TokenService : ITokenService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TokenService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Task<string> GetTokenAsync()
        {
            // Получаем токен из текущего HTTP-контекста
            var authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                return Task.FromResult(authHeader.Substring("Bearer ".Length).Trim());
            }

            return Task.FromResult<string>(string.Empty);
        }
    }
}
