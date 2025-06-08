using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Shared.DTOs.Identity;

namespace MauiClient.Services
{
    // Реалізація сервісу автентифікації
    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly ITokenService _tokenService;

        public AuthService(HttpClient httpClient, ITokenService tokenService)
        {
            _httpClient = httpClient;
            _tokenService = tokenService;
        }

        public async Task<bool> RegisterAsync(RegisterDto model)
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/register", model);
            return response.IsSuccessStatusCode;
        }

        public async Task<string?> LoginAsync(LoginDto model)
        {
            model.DeviceName = DeviceInfo.Name;
            model.DeviceType = DeviceInfo.DeviceType.ToString();
            model.OperatingSystem = DeviceInfo.Platform.ToString();
            model.OsVersion = DeviceInfo.VersionString;

            var response = await _httpClient.PostAsJsonAsync("api/auth/login", model);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AuthDto>();
                if (result != null)
                {
                    await _tokenService.SetTokenAsync(result.Token);
                    await _tokenService.SetRefreshTokenAsync(result.RefreshToken, result.RefreshTokenExpiresAt);
                    return result?.Token;
                }
            }
            return null;
        }

        public async Task LogoutAsync()
        {
            await _tokenService.RemoveTokenAsync();
            await _tokenService.RemoveRefreshTokenAsync();
        }
    }
}
