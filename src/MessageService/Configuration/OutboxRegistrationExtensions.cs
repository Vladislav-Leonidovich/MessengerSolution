using Microsoft.EntityFrameworkCore;
using Shared.Outbox;

namespace MessageService.Configuration
{
    public static class OutboxRegistrationExtensions
    {
        public static IServiceCollection AddOutboxProcessing<TDbContext, TProcessor>(
            this IServiceCollection services,
            Action<OutboxOptions>? configureOptions = null)
            where TDbContext : DbContext
            where TProcessor : class, IHostedService
        {
            // Налаштування опцій
            var options = new OutboxOptions();
            configureOptions?.Invoke(options);
            services.AddSingleton(options);

            // Реєстрація процесора та сервісу
            services.AddSingleton<OutboxProcessor<TDbContext>>();
            services.AddHostedService<TProcessor>();

            return services;
        }
    }
}
