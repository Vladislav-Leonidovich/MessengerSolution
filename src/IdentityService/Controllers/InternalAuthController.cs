using IdentityService.Services.Interfaces;
using MassTransit.JobService;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Controllers
{
    [ApiController]
    [Route("api/internal/auth")]
    public class InternalAuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<InternalAuthController> _logger;
        private readonly IAuthService _authService;

        public InternalAuthController(
            IConfiguration configuration,
            ILogger<InternalAuthController> logger,
            IAuthService authService)
        {
            _configuration = configuration;
            _logger = logger;
            _authService = authService;
        }

        [HttpPost("token")]
        public async Task<IActionResult> GetServiceToken([FromBody] ServiceTokenRequest request)
        {
            // Перевіряємо API ключ
            var apiKey = Request.Headers["X-API-Key"].ToString();
            var configuredApiKey = _configuration["ServiceTokens:ApiKey"];

            if (string.IsNullOrEmpty(apiKey) || apiKey != configuredApiKey)
            {
                _logger.LogWarning("Спроба отримати сервісний токен з неправильним API ключем");
                return Unauthorized();
            }

            // Перевіряємо, чи сервіс має право на отримання токена
            var allowedServices = _configuration.GetSection("ServiceTokens:AllowedServices").Get<string[]>();
            if (allowedServices == null || !allowedServices.Contains(request.ServiceName))
            {
                _logger.LogWarning("Сервіс {ServiceName} не має права на отримання токена", request.ServiceName);
                return Forbid();
            }

            // Генеруємо токен для сервісу використовуючи наш AuthService
            var serviceToken = await _authService.GenerateServiceTokenAsync(request.ServiceName);

            return Ok(new { Token = serviceToken });
        }

        public class ServiceTokenRequest
        {
            public string ServiceName { get; set; }
        }
    }
}
