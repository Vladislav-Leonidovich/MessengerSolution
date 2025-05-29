using Microsoft.AspNetCore.Mvc;
using IdentityService.Models;
using Shared.DTOs.Identity;
using IdentityService.Services.Interfaces;

namespace IdentityService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // POST: api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            try
            {
                var user = await _authService.RegisterAsync(model);
                return Ok(new { Message = "Користувач успішно зареєстрований", UserId = user.Id });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            var authResponse = await _authService.LoginAsync(model, ipAddress);
            if (authResponse == null)
            {
                return Unauthorized(new { Message = "Неправильне ім'я користувача або пароль" });
            }
            return Ok(authResponse);
        }

        // POST: api/auth/refresh
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto request)
        {
            string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            var authResponse = await _authService.RefreshTokenAsync(request.RefreshToken, ipAddress);
            if (authResponse == null)
            {
                return Unauthorized(new { Message = "Invalid or expired refresh token." });
            }

            return Ok(authResponse);
        }
    }
}

