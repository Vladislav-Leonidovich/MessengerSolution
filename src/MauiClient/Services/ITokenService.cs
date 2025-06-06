﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiClient.Services
{
    public interface ITokenService
    {
        Task SetTokenAsync(string token);
        Task<string?> GetTokenAsync();
        Task RemoveTokenAsync();
        Task SetRefreshTokenAsync(string refreshToken, DateTime expiration);
        Task<string?> GetRefreshTokenAsync();
        Task<DateTime?> GetRefreshTokenExpirationAsync();
        Task RemoveRefreshTokenAsync();
    }
}
