namespace ChatService.Configuration
{
    public static class HttpClientExtensions
    {
        /// <summary>
        /// Додає типізовані HTTP клієнти для міжсервісної комунікації
        /// </summary>
        public static IServiceCollection AddServiceHttpClients(
            this IServiceCollection services,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            // REST API клієнт для IdentityService
            services.AddHttpClient("IdentityServiceClient", client =>
            {
                client.BaseAddress = new Uri(configuration["ServiceTokens:IdentityServiceUrl"]);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            })
            .ConfigurePrimaryHttpMessageHandler(() => CreateHandler(environment));

            // gRPC клієнти
            services.AddHttpClient("MessageGrpcClient", client =>
            {
                client.DefaultRequestHeaders.Add("Accept", "application/grpc");
            })
            .ConfigurePrimaryHttpMessageHandler(() => CreateHandler(environment));

            services.AddHttpClient("IdentityGrpcClient", client =>
            {
                client.DefaultRequestHeaders.Add("Accept", "application/grpc");
            })
            .ConfigurePrimaryHttpMessageHandler(() => CreateHandler(environment));

            return services;
        }

        /// <summary>
        /// Створює HTTP обробник з правильними налаштуваннями SSL для середовища
        /// </summary>
        private static HttpClientHandler CreateHandler(IWebHostEnvironment environment)
        {
            var handler = new HttpClientHandler();
            if (environment.IsDevelopment())
            {
                handler.ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

                // Додаткові налаштування для розробки
                handler.MaxConnectionsPerServer = 20;
            }
            else
            {
                // Налаштування для продакшн
                handler.MaxConnectionsPerServer = 100;
                // Можна додати інші оптимізації
            }

            return handler;
        }
    }
}
