using MessageService.BackgroundServices;

namespace MessageService.Configuration
{
    public static class BackgroundServiceExtensions
    {
        /// <summary>
        /// Додає фонові сервіси для обробки Outbox повідомлень
        /// </summary>
        /// <param name="services">Колекція сервісів</param>
        /// <returns>Колекція сервісів</returns>
        public static IServiceCollection AddBackgroundServices(this IServiceCollection services)
        {
            // Сервіс для очистки старих Outbox повідомлень
            services.AddHostedService<OutboxCleanupService>();

            // Сервіс для очистки подій, які були успішно оброблені
            services.AddHostedService<ProcessedEventCleanupService>();

            return services;
        }
    }
}
