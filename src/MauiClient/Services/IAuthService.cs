using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IdentityService.DTOs;

namespace MauiClient.Services
{
    // Інтерфейс для роботи з автентифікацією
    public interface IAuthService
    {
        Task<bool> RegisterAsync(RegisterDto model);
        Task<string?> LoginAsync(LoginDto model);
        Task<bool> IsUserLoggedInAsync();
        Task<bool> LogoutAsync();
        Task<bool> RefreshTokenAsync();
    }
}
