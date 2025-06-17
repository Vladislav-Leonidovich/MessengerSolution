using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace ChatService.Configuration
{
    public static class AuthenticationExtensions
    {
        /// <summary>
        /// Додає налаштування JWT автентифікації
        /// </summary>
        /// <param name="services">Колекція сервісів</param>
        /// <param name="configuration">Конфігурація застосунку</param>
        /// <returns>Колекція сервісів</returns>
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSecretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
                             ?? configuration["JWT_SECRET_KEY"];

            if (string.IsNullOrEmpty(jwtSecretKey))
            {
                throw new InvalidOperationException(
                    "JWT Secret Key не налаштовано. Встановіть змінну середовища JWT_SECRET_KEY або додайте в конфігурацію");
            }

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false; // В production встановити true
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false, // В production налаштувати issuer
                    ValidateAudience = false, // В production налаштувати audience
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
                    ClockSkew = TimeSpan.Zero // Зменшуємо допустиме відхилення часу
                };

                // Налаштування для SignalR
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;

                        // Дозволяємо передачу токена через query string для SignalR
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            return services;
        }

        /// <summary>
        /// Додає налаштування авторизації
        /// </summary>
        /// <param name="services">Колекція сервісів</param>
        /// <returns>Колекція сервісів</returns>
        public static IServiceCollection AddCustomAuthorization(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                // Базова політика - користувач повинен бути автентифікований
                //options.FallbackPolicy = options.DefaultPolicy;

                // Можна додати додаткові політики
                options.AddPolicy("RequireAdminRole", policy =>
                    policy.RequireClaim("role", "Admin"));
            });

            return services;
        }
    }
}
