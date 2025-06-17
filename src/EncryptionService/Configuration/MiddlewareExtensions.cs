using EncryptionService.Middleware;

namespace ChatService.Configuration
{
    public static class MiddlewareExtensions
    {
        /// <summary>
        /// Додає middleware для обробки помилок
        /// </summary>
        /// <param name="app">Будівник застосунку</param>
        /// <returns>Будівник застосунку</returns>
        public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app)
        {
            app.UseMiddleware<ExceptionHandlingMiddleware>();
            return app;
        }

        /// <summary>
        /// Налаштовує базовий pipeline middleware
        /// </summary>
        /// <param name="app">Будівник застосунку</param>
        /// <param name="env">Середовище виконання</param>
        /// <returns>Будівник застосунку</returns>
        public static IApplicationBuilder UseBasicMiddleware(this IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Обробка помилок повинна бути першою
            app.UseGlobalExceptionHandling();

            // Development-specific middleware
            if (env.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Chat Service API V1");
                    options.RoutePrefix = "swagger";
                });
            }

            // Security headers
            app.Use(async (context, next) =>
            {
                context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
                context.Response.Headers.Add("X-Frame-Options", "DENY");
                context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
                await next();
            });

            // CORS (якщо потрібно)
            app.UseCors(policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });

            app.UseRouting();

            // Автентифікація та авторизація
            app.UseAuthentication();
            app.UseAuthorization();

            return app;
        }
    }
}
