using Grpc.AspNetCore.Server;
using Shared.Interceptors;

namespace MessageService.Configuration
{
    public static class GrpcInterceptorExtensions
    {
        /// <summary>
        /// Додає налаштування для gRPC серверних перехоплювачів
        /// </summary>
        /// <param name="services">Колекція сервісів</param>
        /// <returns>Колекція сервісів</returns>
        public static IServiceCollection AddGrpcInterceptors(this IServiceCollection services)
        {
            // Реєстрація серверних перехоплювачів
            services.AddScoped<ServerAuthInterceptor>();

            return services;
        }

        /// <summary>
        /// Налаштовує gRPC сервери з перехоплювачами
        /// </summary>
        /// <param name="services">Колекція сервісів</param>
        /// <param name="environment">Середовище виконання</param>
        /// <returns>Колекція сервісів</returns>
        public static IServiceCollection AddGrpcServers(this IServiceCollection services, IWebHostEnvironment environment)
        {
            services.AddGrpc(options =>
            {
                // Базові налаштування gRPC
                options.MaxReceiveMessageSize = 16 * 1024 * 1024; // 16MB
                options.MaxSendMessageSize = 16 * 1024 * 1024;    // 16MB
                options.EnableDetailedErrors = environment.IsDevelopment();

                // Глобальні перехоплювачі
                options.Interceptors.Add<ServerAuthInterceptor>();
            });

            return services;
        }

        public static IServiceCollection AddGrpcServiceOptions<TService>(
            this IServiceCollection services,
            Action<GrpcServiceOptions> configureOptions)
            where TService : class
        {
            return services.Configure<GrpcServiceOptions>(
                typeof(TService).Name,
                configureOptions);
        }
    }
}
