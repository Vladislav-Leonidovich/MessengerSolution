using ChatService.Authorization;
using ChatService.Mappers.Interfaces;
using ChatService.Mappers;
using ChatService.Repositories.Interfaces;
using ChatService.Repositories;
using ChatService.Services.Interfaces;
using ChatService.Services;
using Shared.Authorization.Permissions;
using Shared.Authorization;
using MessageService.Services;

namespace ChatService.Configuration
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
            services.AddScoped<IChatRoomRepository, ChatRoomRepository>();
            services.AddScoped<IFolderRepository, FolderRepository>();

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
            services.AddScoped<IChatService, Services.ChatService>();
            services.AddScoped<IFolderService, FolderService>();
            services.AddScoped<IChatOperationService, ChatOperationService>();

            // gRPC сервіси
            services.AddScoped<IMessageGrpcClient, MessageGrpcClient>();
            services.AddScoped<IIdentityGrpcClient, IdentityGrpcClient>();

            // Допоміжні сервіси
            services.AddScoped<ITokenService, TokenService>();
            services.AddHttpContextAccessor();

            return services;
        }

        /// <summary>
        /// Реєструє всі mapper'и
        /// </summary>
        /// <param name="services">Колекція сервісів</param>
        /// <returns>Колекція сервісів</returns>
        public static IServiceCollection AddMappers(this IServiceCollection services)
        {
            // Фабрика mapper'ів
            services.AddScoped<IMapperFactory, MapperFactory>();

            // Реєстрація всіх mapper'ів
            services.AddScoped<IEntityMapper<ChatService.Models.PrivateChatRoom, Shared.DTOs.Chat.ChatRoomDto>, PrivateChatRoomMapper>();
            services.AddScoped<IEntityMapper<ChatService.Models.GroupChatRoom, Shared.DTOs.Chat.GroupChatRoomDto>, GroupChatRoomMapper>();
            services.AddScoped<IEntityMapper<ChatService.Models.GroupChatMember, Shared.DTOs.Chat.GroupChatMemberDto>, GroupChatMemberMapper>();
            services.AddScoped<IEntityMapper<ChatService.Models.Folder, Shared.DTOs.Folder.FolderDto>, FolderMapper>();
            services.AddScoped<IEntityMapper<Shared.Protos.MessageData, Shared.DTOs.Message.MessageDto>, MessageMapper>();
            services.AddScoped<IEntityMapper<Shared.Protos.UserData, Shared.DTOs.Identity.UserDto>, UserMapper>();

            return services;
        }

        /// <summary>
        /// Реєструє сервіси авторизації
        /// </summary>
        /// <param name="services">Колекція сервісів</param>
        /// <returns>Колекція сервісів</returns>
        public static IServiceCollection AddAuthorizationServices(this IServiceCollection services)
        {
            services.AddScoped<IChatAuthorizationService, ChatAuthorizationService>();
            services.AddScoped<IPermissionService<ChatPermission>, ChatPermissionService>();

            return services;
        }

        /// <summary>
        /// Реєструє HTTP клієнти
        /// </summary>
        /// <param name="services">Колекція сервісів</param>
        /// <param name="configuration">Конфігурація</param>
        /// <returns>Колекція сервісів</returns>
        public static IServiceCollection AddHttpClients(this IServiceCollection services, IConfiguration configuration)
        {
            // HTTP клієнт для Identity Service
            services.AddHttpClient("IdentityClient", client =>
            {
                var baseUrl = configuration["Services:IdentityService:BaseUrl"] ?? "https://localhost:7101";
                client.BaseAddress = new Uri(baseUrl);
                client.DefaultRequestHeaders.Add("User-Agent", "ChatService/1.0");
            });

            // HTTP клієнт для Message Service
            services.AddHttpClient("MessageService", client =>
            {
                var baseUrl = configuration["Services:MessageService:BaseUrl"] ?? "https://localhost:7103";
                client.BaseAddress = new Uri(baseUrl);
                client.DefaultRequestHeaders.Add("User-Agent", "ChatService/1.0");
            });

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
