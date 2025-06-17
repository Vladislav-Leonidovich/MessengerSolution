namespace EncryptionService.Configuration
{
    public static class ExceptionHandlingExtensions
    {
        /// <summary>
        /// Додає налаштування для централізованої обробки помилок
        /// </summary>
        /// <param name="services">Колекція сервісів</param>
        /// <returns>Колекція сервісів</returns>
        public static IServiceCollection AddExceptionHandling(this IServiceCollection services)
        {
            // Налаштування ModelState validation
            services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
            {
                // Відключаємо автоматичну обробку помилок валідації
                // Щоб наш middleware міг їх обробляти
                options.SuppressModelStateInvalidFilter = false;

                // Можна налаштувати кастомну обробку помилок валідації
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState
                        .Where(x => x.Value?.Errors.Count > 0)
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                        );

                    var response = new
                    {
                        success = false,
                        message = "Помилка валідації даних",
                        errors = errors
                    };

                    return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(response);
                };
            });

            return services;
        }
    }
}
