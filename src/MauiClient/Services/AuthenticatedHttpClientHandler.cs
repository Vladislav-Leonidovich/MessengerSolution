using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MauiClient.Services
{
    public class AuthenticatedHttpClientHandler : DelegatingHandler
    {
        private readonly IAuthService _authService;
        private readonly ITokenService _tokenService;

        public AuthenticatedHttpClientHandler(IAuthService authService, ITokenService tokenService)
        {
            _tokenService = tokenService;
            _authService = authService;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await _tokenService.GetTokenAsync();
            if (!string.IsNullOrEmpty(token) && await _authService.IsUserLoggedInAsync())
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
            var response = await base.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                var refreshResult = await _authService.RefreshTokenAsync();
                if (refreshResult)
                {
                    var newToken = await _tokenService.GetTokenAsync();

                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
                    response = await base.SendAsync(request, cancellationToken);
                }
            }
            return response;
        }
    }
}
