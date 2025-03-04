using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace MauiClient.Services
{
    public class SecureTokenService : ITokenService
    {
        private const string TokenKey = "authToken";
        private const string RefreshTokenKey = "RefreshToken";

        public async Task SetTokenAsync(string token)
        {
            await SecureStorage.SetAsync(TokenKey, token);
        }

        public async Task SetRefreshTokenAsync(string refreshToken)
        {
            await SecureStorage.SetAsync(RefreshTokenKey, refreshToken);
        }

        public async Task<string?> GetTokenAsync()
        {
            try
            {
                return await SecureStorage.GetAsync(TokenKey);
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
                return await SecureStorage.GetAsync(RefreshTokenKey);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to get refresh token", ex);
            }
        }

        public Task RemoveTokenAsync()
        {
            SecureStorage.Remove(TokenKey);
            return Task.CompletedTask;
        }

        public Task RemoveRefreshTokenAsync()
        {
            SecureStorage.Remove(RefreshTokenKey);
            return Task.CompletedTask;
        }
    }
}
