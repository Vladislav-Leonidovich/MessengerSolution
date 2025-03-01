using System.Net.Http.Headers;

namespace MessageService.Services
{
    public class InternalAuthHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public InternalAuthHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Отримуємо токен з поточного HTTP-контексту
            var authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrEmpty(authHeader))
            {
                // Якщо токен вже містить "Bearer", використовуйте його
                request.Headers.Authorization = AuthenticationHeaderValue.Parse(authHeader);
            }
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
