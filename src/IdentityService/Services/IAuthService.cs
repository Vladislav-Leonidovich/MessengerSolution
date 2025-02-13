using IdentityService.DTOs;
using IdentityService.Models;

namespace IdentityService.Services
{
    public interface IAuthService
    {
        Task<bool> UserExistsAsync(string username, string email);
        Task<User> RegisterAsync(RegisterModel model);
        Task<AuthResponse?> LoginAsync(LoginModel model);
    }
}
