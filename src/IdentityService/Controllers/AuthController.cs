using Microsoft.AspNetCore.Mvc;
using IdentityService.Models;
using IdentityService.Services;
using IdentityService.DTOs;

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
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
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
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            var authResponse = await _authService.LoginAsync(model);
            if (authResponse == null)
            {
                return Unauthorized(new { Message = "Неправильне ім'я користувача або пароль" });
            }
            return Ok(authResponse);
        }
    }
}

