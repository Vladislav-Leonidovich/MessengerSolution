using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Maui.Storage;

namespace MauiClient.Services
{
    public class SecureTokenService : ITokenService
    {
        private const string TokenKey = "authToken";
        private const string RefreshTokenKey = "RefreshToken";
        private const string RefreshTokenExpirationKey = "RefreshTokenExpiration";

        public async Task SetTokenAsync(string token)
        {
            await SecureStorage.Default.SetAsync(TokenKey, token);
        }

        public async Task SetRefreshTokenAsync(string refreshToken, DateTime expiration)
        {
            await SecureStorage.Default.SetAsync(RefreshTokenKey, refreshToken);
            // Зберігаємо дату закінчення в форматі ISO 8601
            await SecureStorage.Default.SetAsync(RefreshTokenExpirationKey, expiration.ToString("o"));
        }

        public async Task<string?> GetTokenAsync()
        {
            try
            {
                return await SecureStorage.Default.GetAsync(TokenKey);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to get token", ex);
            }
        }

        public async Task<string?> GetRefreshTokenAsync()
        {
            try
            {
                return await SecureStorage.Default.GetAsync(RefreshTokenKey);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to get refresh token", ex);
            }
        }

        public async Task<DateTime?> GetRefreshTokenExpirationAsync()
        {
            try
            {
                var expirationStr = await SecureStorage.Default.GetAsync(RefreshTokenExpirationKey);
                if (DateTime.TryParse(expirationStr, out DateTime expiration))
                {
                    return expiration;
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to get refresh token expiration", ex);
            }
        }

        public Task RemoveTokenAsync()
        {
            SecureStorage.Default.Remove(TokenKey);
            return Task.CompletedTask;
        }

        public Task RemoveRefreshTokenAsync()
        {
            SecureStorage.Default.Remove(RefreshTokenKey);
            SecureStorage.Default.Remove(RefreshTokenExpirationKey);
            return Task.CompletedTask;
        }
    }
}
