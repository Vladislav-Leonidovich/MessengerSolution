using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using IdentityService.DTOs;
using Shared.IdentityServiceDTOs;

namespace MauiClient.Services
{
    // Реалізація сервісу автентифікації
    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly ITokenService _tokenService;

        public AuthService(IHttpClientFactory httpClientFactory, ITokenService tokenService)
        {
            _httpClient = httpClientFactory.CreateClient("RefreshTokenClient");
            _tokenService = tokenService;
        }

        public async Task<bool> RegisterAsync(RegisterDto model)
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/register", model);
            return response.IsSuccessStatusCode;
        }

        public async Task<string?> LoginAsync(LoginDto model)
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", model);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AuthDto>();
                if (result != null)
                {
                    await _tokenService.SetTokenAsync(result.Token);
                    await _tokenService.SetRefreshTokenAsync(result.RefreshToken);
                    return result?.Token;
                }
            }
            return null;
        }

        public async Task<bool> IsUserLoggedInAsync()
        {
            var token = await _tokenService.GetTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }
            if (IsJwtExpired(token))
            {
                var RefreshResult = await RefreshTokenAsync();
                if (!RefreshResult)
                {
                    await LogoutAsync();
                    return false;
                }
            }

            return true;
        }

        public async Task<bool> LogoutAsync()
        {
            await _tokenService.SetTokenAsync(string.Empty);

            if (await IsUserLoggedInAsync())
            {
                return false;
            }

            return true;
        }

        public async Task<bool> RefreshTokenAsync()
        {
            var token = await _tokenService.GetTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }

            if (!IsJwtExpired(token))
            {
                return false;
            }

            var refreshToken = await _tokenService.GetRefreshTokenAsync();
            if (string.IsNullOrEmpty(refreshToken))
            {
                return false;
            }

            var response = await _httpClient.PostAsJsonAsync("api/auth/refresh", new RefreshTokenDto { RefreshToken = refreshToken });
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AuthDto>();
                if (result != null)
                {
                    await _tokenService.SetTokenAsync(result.Token);
                    await _tokenService.SetRefreshTokenAsync(result.RefreshToken);
                    return true;
                }
            }
            return false;
        }

        private bool IsJwtExpired(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var expClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "exp")?.Value;
            if (long.TryParse(expClaim, out long expSeconds))
            {
                var expirationTime = DateTimeOffset.FromUnixTimeSeconds(expSeconds).UtcDateTime;
                return DateTime.UtcNow >= expirationTime;
            }
            return true;
        }
    }
}
