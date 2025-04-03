using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace MauiClient.Services
{
    public class ClientAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly ITokenRefresher _tokenRefresher;
        private readonly ITokenService _tokenService;
        private readonly NavigationManager _navigation;

        public ClientAuthenticationStateProvider(ITokenRefresher tokenRefresher, ITokenService tokenService, NavigationManager navigation)
        {
            _tokenRefresher = tokenRefresher;
            _tokenService = tokenService;
            _navigation = navigation;
        }

        public async Task Login(string token)
        {
            await _tokenService.SetTokenAsync(token);
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        public async Task Logout()
        {
            await _tokenService.RemoveTokenAsync();
            await _tokenService.RemoveRefreshTokenAsync();
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            // За замовчуванням – неавторизований користувач
            var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
            string? token = await _tokenService.GetTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                return new AuthenticationState(anonymous);
            }
            // Якщо токен є – формуємо ClaimsPrincipal з нього
            var handler = new JwtSecurityTokenHandler();
            JwtSecurityToken jwt;
            try
            {
                jwt = handler.ReadJwtToken(token);
            }
            catch
            {
                // Неприпустимий токен
                await Logout();
                return new AuthenticationState(anonymous);
            }
            // Перевіряємо строк придатності токена
            if (jwt.ValidTo < DateTime.UtcNow)
            {
                // Тут можна виконати спробу оновлення токена за допомогою refresh-токена
                string? refresh = await _tokenService.GetRefreshTokenAsync();
                if (!string.IsNullOrEmpty(refresh))
                {
                    var isRefresh = await _tokenRefresher.RefreshTokenAsync(refresh);
                    if(isRefresh)
                    {
                        token = await _tokenService.GetTokenAsync();
                        jwt = handler.ReadJwtToken(token);
                    }
                    else
                    {
                        await Logout();
                        return new AuthenticationState(anonymous);
                    }
                }
            }
            // Створюємо ClaimsIdentity з отриманих з JWT тверджень
            var identity = new ClaimsIdentity(jwt.Claims, "Bearer");
            var user = new ClaimsPrincipal(identity);
            return new AuthenticationState(user);
        }
    }
}
