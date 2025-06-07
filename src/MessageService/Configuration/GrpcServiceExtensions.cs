using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using MessageService.Services;
using MessageService.Services.Interfaces;
using Shared.Interceptors;
using Shared.Protos;

namespace MessageService.Configuration
{
    public static class GrpcServiceExtensions
    {
        /// <summary>
        /// Додає gRPC-клієнти до DI-контейнера
        /// </summary>
        public static IServiceCollection AddGrpcClients(this IServiceCollection services, IConfiguration configuration)
        {
            ConfigureGrpcClient<ChatGrpcService.ChatGrpcServiceClient>(
                services, configuration["GrpcServices:ChatService"]);

            ConfigureGrpcClient<EncryptionGrpcService.EncryptionGrpcServiceClient>(
                services, configuration["GrpcServices:EncryptionService"]);

            return services;
        }

        private static void ConfigureGrpcClient<TClient>(IServiceCollection services, string serviceUrl)
    where TClient : ClientBase<TClient>
        {
            if (string.IsNullOrEmpty(serviceUrl))
            {
                throw new ArgumentException($"URL для {typeof(TClient).Name} не налаштовано у конфігурації");
            }

            services.AddScoped(serviceProvider =>
            {
                var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
                var tokenService = serviceProvider.GetRequiredService<ITokenService>();
                var httpClient = httpClientFactory.CreateClient(typeof(TClient).Name);

                var channel = GrpcChannel.ForAddress(serviceUrl, new GrpcChannelOptions
                {
                    HttpClient = httpClient,
                    MaxReceiveMessageSize = 16 * 1024 * 1024,
                    MaxSendMessageSize = 16 * 1024 * 1024
                });

                Func<Task<string>> tokenProvider = async () => await tokenService.GetTokenAsync();
                var logger = serviceProvider.GetRequiredService<ILogger<AuthGrpcInterceptor>>();
                var interceptor = new AuthGrpcInterceptor(tokenProvider, logger);

                return (TClient)Activator.CreateInstance(typeof(TClient), channel.Intercept(interceptor));
            });
        }
    }
}
