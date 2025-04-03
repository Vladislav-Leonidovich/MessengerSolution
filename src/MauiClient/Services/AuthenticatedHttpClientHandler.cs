using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace MauiClient.Services
{
    public class AuthenticatedHttpClientHandler : DelegatingHandler
    {
        private readonly ITokenRefresher _tokenRefresher;
        private readonly ITokenService _tokenService;
        private readonly ClientAuthenticationStateProvider _clientAuthenticationStateProvider;

        public AuthenticatedHttpClientHandler(ITokenRefresher tokenRefresher, ITokenService tokenService, ClientAuthenticationStateProvider clientAuthenticationStateProvider)
        {
            _tokenService = tokenService;
            _tokenRefresher = tokenRefresher;
            _clientAuthenticationStateProvider = clientAuthenticationStateProvider;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await _tokenService.GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            // Відправляємо запит
            var response = await base.SendAsync(request, cancellationToken);

            // Перевірка на статус 401 Unauthorized
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                // Якщо токен не дійсний, намагаємось оновити його
                var refreshToken = await _tokenService.GetRefreshTokenAsync();
                if (!string.IsNullOrEmpty(refreshToken))
                {
                    var refreshResult = await _tokenRefresher.RefreshTokenAsync(refreshToken);
                    if (refreshResult)
                    {
                        // Якщо оновлення успішне, отримуємо новий токен
                        var newToken = await _tokenService.GetTokenAsync();
                        // Повторюємо запит з новим токеном
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
                        // Повторно виконуємо запит з новим токеном
                        return await base.SendAsync(request, cancellationToken);
                    }
                }
                // Якщо оновлення не вдалося, виконуємо логін/вихід
                await _clientAuthenticationStateProvider.Logout();
            }
            return response;
        }
    }
}
