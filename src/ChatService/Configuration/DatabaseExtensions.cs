using ChatService.Data;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Configuration
{
    public static class DatabaseExtensions
    {
        /// <summary>
        /// Додає налаштування Entity Framework та бази даних
        /// </summary>
        /// <param name="services">Колекція сервісів</param>
        /// <param name="configuration">Конфігурація застосунку</param>
        /// <returns>Колекція сервісів</returns>
        public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            // Налаштування Entity Framework з MySQL
            services.AddDbContext<ChatDbContext>(options =>
            {
                var connectionString = configuration.GetConnectionString("ChatDatabase");
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException("Рядок підключення 'ChatDatabase' не знайдено в конфігурації");
                }

                options.UseMySQL(connectionString);

                // Налаштування для Development середовища
                if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
                {
                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
                }
            });

            return services;
        }

        /// <summary>
        /// Автоматично застосовує міграції бази даних
        /// </summary>
        /// <param name="app">Будівник застосунку</param>
        /// <returns>Будівник застосунку</returns>
        public static IApplicationBuilder UseDatabaseMigration(this IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ChatDbContext>();

            try
            {
                context.Database.Migrate();
            }
            catch (Exception ex)
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "Помилка під час застосування міграцій бази даних");
                throw;
            }

            return app;
        }
    }
}
