using ChatService.Repositories;
using ChatService.Services.Interfaces;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using Shared.Interceptors;
using Shared.Protos;

namespace ChatService.Configuration
{
    public static class GrpcServiceExtensions
    {
        /// <summary>
        /// Додає gRPC-клієнти до DI-контейнера
        /// </summary>
        public static IServiceCollection AddGrpcClients(this IServiceCollection services, IConfiguration configuration)
        {
            // Отримуємо URL MessageService з конфігурації
            var messageServiceUrl = configuration["GrpcServices:MessageService"];
            if (string.IsNullOrEmpty(messageServiceUrl))
            {
                throw new ArgumentException("URL для MessageService не налаштовано у конфігурації");
            }

            // Змінено з AddSingleton на AddScoped
            services.AddScoped(serviceProvider =>
            {
                var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
                var tokenService = serviceProvider.GetRequiredService<ITokenService>();
                var httpClient = httpClientFactory.CreateClient("MessageServiceGrpc");
                var channel = GrpcChannel.ForAddress(messageServiceUrl, new GrpcChannelOptions
                {
                    HttpClient = httpClient,
                    MaxReceiveMessageSize = 100 * 1024 * 1024, // 100 MB
                    MaxSendMessageSize = 100 * 1024 * 1024     // 100 MB
                });

                // Створюємо функцію для отримання токена
                Func<Task<string>> tokenProvider = async () =>
                {
                    return await tokenService.GetTokenAsync();
                };

                var logger = serviceProvider.GetRequiredService<ILogger<AuthGrpcInterceptor>>();
                var interceptor = new AuthGrpcInterceptor(tokenProvider, logger);
                return new MessageGrpcService.MessageGrpcServiceClient(channel.Intercept(interceptor));
            });

            // Реєструємо сервіс
            services.AddScoped<IMessageGrpcService, Services.MessageGrpcService>();
            return services;
        }
    }
}
