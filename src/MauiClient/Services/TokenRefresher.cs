using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Shared.DTOs.Identity;

namespace MauiClient.Services
{
    public class TokenRefresher : ITokenRefresher
    {
        private readonly ITokenService _tokenService;
        private readonly HttpClient _httpClient;
        public TokenRefresher(ITokenService tokenService, IHttpClientFactory httpClientFactory) 
        {
            _tokenService = tokenService;
            _httpClient = httpClientFactory.CreateClient("RefreshTokenClient");
        }

        public async Task<bool> RefreshTokenAsync(string refresh)
        {
            if (await isRefreshTokenExpired())
            {
                return false;
            }

            var response = await _httpClient.PostAsJsonAsync("api/auth/refresh", new RefreshTokenDto { RefreshToken = refresh });
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AuthDto>();
                if (result != null)
                {
                    await _tokenService.SetTokenAsync(result.Token);
                    await _tokenService.SetRefreshTokenAsync(result.RefreshToken, result.RefreshTokenExpiresAt);
                    return true;
                }
            }
            return false;
        }

        private async Task<bool> isRefreshTokenExpired()
        {
            var expiration = await _tokenService.GetRefreshTokenExpirationAsync();
            if (expiration == null)
            {
                return true;
            }
            if (expiration < DateTime.UtcNow)
            {
                return true;
            }
            return false;
        }

        private bool IsJwtExpired(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            if (jwtToken.ValidTo < DateTime.UtcNow)
            {
                return true;
            }
            return false;
        }
    }
}
