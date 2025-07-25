﻿using IdentityService.Models;
using Shared.DTOs.Identity;

namespace IdentityService.Services.Interfaces
{
    public interface IAuthService
    {
        Task<bool> UserExistsAsync(string username, string email);
        Task<User> RegisterAsync(RegisterDto model);
        Task<AuthDto?> LoginAsync(LoginDto model, string ipAddress);
        Task<AuthDto?> RefreshTokenAsync(string currentRefreshToken, string ipAddress);
        Task<string> GenerateServiceTokenAsync(string serviceName);
    }
}
