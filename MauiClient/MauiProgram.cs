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
            // Реєструємо DelegatingHandler
            builder.Services.AddTransient<AuthenticatedHttpClientHandler>();

            // Налаштування іменованого HttpClient через HttpClientFactory з базовою адресою та обробником
            builder.Services.AddHttpClient("ApiClient", client =>
            {
                client.BaseAddress = new Uri("https://localhost:7100/");
            })
            .AddHttpMessageHandler<AuthenticatedHttpClientHandler>();

            // Для зручного доступу реєструємо HttpClient по замовчуванню:
            builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("ApiClient"));
            // Реєстрація сервісів API
            builder.Services.AddScoped<ITokenService, SecureTokenService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IChatService, ChatService>();
            builder.Services.AddScoped<IFolderService, FolderService>();

            builder.Services.AddMauiBlazorWebView();

#if DEBUG
    		builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
