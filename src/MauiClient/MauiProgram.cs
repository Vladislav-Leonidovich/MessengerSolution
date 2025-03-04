using MauiClient.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;

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

            // Реєстрація сервісів API
            builder.Services.AddScoped<ITokenService, SecureTokenService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IChatService, ChatService>();
            builder.Services.AddScoped<IFolderService, FolderService>();

            // Реєстрація AuthenticatedHttpClientHandler через фабрику
            builder.Services.AddTransient<AuthenticatedHttpClientHandler>(sp =>
            {
                var authService = sp.GetRequiredService<IAuthService>();
                var tokenService = sp.GetRequiredService<ITokenService>();
                return new AuthenticatedHttpClientHandler(authService, tokenService);
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

            // Для зручного доступу реєструємо HttpClient по замовчуванню:
            builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("ApiClient"));

            builder.Services.AddMauiBlazorWebView();

#if DEBUG
    		builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
