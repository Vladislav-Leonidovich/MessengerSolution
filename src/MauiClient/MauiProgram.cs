using MauiClient.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace MauiClient
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            // Налаштування іменованого HttpClient через HttpClientFactory з базовою адресою та обробником
            builder.Services.AddHttpClient("ApiClient", client =>
            {
                client.BaseAddress = new Uri("https://localhost:7100/");
            })
            .AddHttpMessageHandler<AuthenticatedHttpClientHandler>();

            builder.Services.AddHttpClient("RefreshTokenClient", client =>
            {
                client.BaseAddress = new Uri("https://localhost:7100/");
            });

            builder.Services.AddHttpClient("AuthClient", client =>
            {
                client.BaseAddress = new Uri("https://localhost:7100/");
            });

            // Реєстрація сервісів API
            builder.Services.AddScoped<ITokenService, SecureTokenService>();
            builder.Services.AddScoped<IAuthService, AuthService>(sp =>
            {
                var clientFactory = sp.GetRequiredService<IHttpClientFactory>();
                var httpClient = clientFactory.CreateClient("AuthClient");
                var tokenService = sp.GetRequiredService<ITokenService>();
                return new AuthService(httpClient, tokenService);
            });
            builder.Services.AddScoped<IChatService, ChatService>();
            builder.Services.AddScoped<IFolderService, FolderService>();
            builder.Services.AddScoped<ITokenRefresher, TokenRefresher>();
            builder.Services.AddScoped<IMessageService, MessageService>();
            builder.Services.AddScoped<IUserService, UserService>();

            builder.Services.AddAuthorizationCore();
            builder.Services.AddScoped<ClientAuthenticationStateProvider>();
            builder.Services.AddScoped<AuthenticationStateProvider>(
                provider => provider.GetRequiredService<ClientAuthenticationStateProvider>());

            // Реєстрація AuthenticatedHttpClientHandler через фабрику
            builder.Services.AddScoped<AuthenticatedHttpClientHandler>();

            // Для зручного доступу реєструємо HttpClient по замовчуванню:
            builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("ApiClient"));

            builder.Services.AddSingleton<LocalizationResourceManager>();

            builder.Services.AddMauiBlazorWebView();

#if DEBUG
    		builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
