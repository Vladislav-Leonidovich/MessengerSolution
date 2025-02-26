using IdentityService.DTOs;
using IdentityService.Models;

namespace IdentityService.Services
{
    public interface IAuthService
    {
        Task<bool> UserExistsAsync(string username, string email);
        Task<User> RegisterAsync(RegisterDto model);
        Task<AuthDto?> LoginAsync(LoginDto model);
    }
}
