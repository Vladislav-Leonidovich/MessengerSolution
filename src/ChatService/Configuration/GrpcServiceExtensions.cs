using ChatService.Repositories;
using ChatService.Services;
using ChatService.Services.Interfaces;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using Microsoft.Extensions.DependencyInjection;
using Shared.Interceptors;
using Shared.Protos;

namespace ChatService.Configuration
{
    public static class GrpcServiceExtensions
    {
        public static IServiceCollection AddGrpcClients(this IServiceCollection services, IConfiguration configuration)
        {
            // Використовуємо вбудовані методи .NET для реєстрації gRPC клієнтів
            services.AddGrpcClient<MessageGrpcService.MessageGrpcServiceClient>(options =>
            {
                options.Address = new Uri(configuration["GrpcServices:MessageGrpcService"]);
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
            services.AddGrpcClient<IdentityGrpcService.IdentityGrpcServiceClient>(options =>
            {
                options.Address = new Uri(configuration["GrpcServices:IdentityGrpcService"]);
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
            services.AddScoped<IMessageGrpcClient, MessageGrpcClient>();
            services.AddScoped<IIdentityGrpcClient, IdentityGrpcClient>();

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

