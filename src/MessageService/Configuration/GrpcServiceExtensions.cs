using System;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using MessageService.Services;
using MessageService.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
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
            // Використовуємо вбудовані методи .NET для реєстрації gRPC клієнтів
            services.AddGrpcClient<ChatGrpcService.ChatGrpcServiceClient>(options =>
            {
                options.Address = new Uri(configuration["GrpcServices:ChatGrpcService"]);
            })
            .ConfigureChannel(channel =>
            {
                channel.MaxReceiveMessageSize = 16 * 1024 * 1024;
                channel.MaxSendMessageSize = 16 * 1024 * 1024;
            })
            .AddCallCredentials(async (context, metadata, serviceProvider) =>
            {
                var tokenService = serviceProvider.GetRequiredService<ITokenService>();
                await AddTokenToMetadataAsync(tokenService, metadata);
            })
            .ConfigurePrimaryHttpMessageHandler(provider =>
            {
                var handler = new HttpClientHandler();
                var environment = provider.GetRequiredService<IWebHostEnvironment>();
                if (environment.IsDevelopment())
                {
                    handler.ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                }
                return handler;
            });

            // Аналогічно для інших клієнтів
            services.AddGrpcClient<EncryptionGrpcService.EncryptionGrpcServiceClient>(options =>
            {
                options.Address = new Uri(configuration["GrpcServices:EncryptionGrpcService"]);
            })
            .ConfigureChannel(channel =>
            {
                channel.MaxReceiveMessageSize = 16 * 1024 * 1024;
                channel.MaxSendMessageSize = 16 * 1024 * 1024;
            })
            .AddCallCredentials(async (context, metadata, serviceProvider) =>
            {
                var tokenService = serviceProvider.GetRequiredService<ITokenService>();
                await AddTokenToMetadataAsync(tokenService, metadata);
            })
            .ConfigurePrimaryHttpMessageHandler(provider =>
            {
                var handler = new HttpClientHandler();
                var environment = provider.GetRequiredService<IWebHostEnvironment>();
                if (environment.IsDevelopment())
                {
                    handler.ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                }
                return handler;
            });

            return services;
        }

        /// <summary>
        /// Реєструє всі обгортки для gRPC клієнтів
        /// </summary>
        public static IServiceCollection AddGrpcClientWrappers(this IServiceCollection services)
        {
            // Реєструємо обгортки
            services.AddScoped<IChatGrpcClient, ChatGrpcClient>();
            services.AddScoped<IEncryptionGrpcClient, EncryptionGrpcClient>();

            return services;
        }

        private static async Task AddTokenToMetadataAsync(ITokenService tokenService, Metadata metadata)
        {
            var token = await tokenService.GetServiceToServiceTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                metadata.Add("Authorization", $"Bearer {token}");
            }
        }
    }
}
