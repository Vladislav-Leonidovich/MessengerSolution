using Shared.Authorization.Permissions;
using Shared.Authorization;
using MessageService.Repositories.Interfaces;
using MessageService.Repositories;
using MessageService.Services.Interfaces;
using MessageService.Services;
using MessageService.Authorization;

namespace MessageService.Configuration
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Реєструє всі репозиторії
        /// </summary>
        /// <param name="services">Колекція сервісів</param>
        /// <returns>Колекція сервісів</returns>
        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddScoped<IMessageRepository, MessageRepository>();

            return services;
        }

        /// <summary>
        /// Реєструє всі бізнес-сервіси
        /// </summary>
        /// <param name="services">Колекція сервісів</param>
        /// <returns>Колекція сервісів</returns>
        public static IServiceCollection AddBusinessServices(this IServiceCollection services)
        {
            // Основні бізнес-сервіси
            services.AddScoped<IMessageService, MessageService.Services.MessageService>();
            services.AddScoped<IEventPublisher, OutboxEventPublisher>();

            // gRPC сервіси
            services.AddScoped<MessageGrpcService>();
            services.AddScoped<IChatGrpcClient, ChatGrpcClient>();
            services.AddScoped<IEncryptionGrpcClient, EncryptionGrpcClient>();

            // Допоміжні сервіси
            services.AddScoped<ITokenService, TokenService>();
            services.AddHttpContextAccessor();

            return services;
        }

        /// <summary>
        /// Реєструє сервіси авторизації
        /// </summary>
        /// <param name="services">Колекція сервісів</param>
        /// <returns>Колекція сервісів</returns>
        public static IServiceCollection AddAuthorizationServices(this IServiceCollection services)
        {
            services.AddScoped<IMessageAuthorizationService, MessageAuthorizationService>();
            services.AddScoped<IPermissionService<MessagePermission>, MessagePermissionService>();

            return services;
        }

        /// <summary>
        /// Додає налаштування Swagger/OpenAPI
        /// </summary>
        /// <param name="services">Колекція сервісів</param>
        /// <returns>Колекція сервісів</returns>
        public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "Chat Service API",
                    Version = "v1",
                    Description = "API для управління чатами та папками"
                });

                // Налаштування для JWT авторизації в Swagger
                options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    {
                        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                        {
                            Reference = new Microsoft.OpenApi.Models.OpenApiReference
                            {
                                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            return services;
        }
    }
}
