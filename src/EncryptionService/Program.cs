using EncryptionService.Middleware;
using EncryptionService.Services;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Конфігурація служб
builder.Services.AddControllers();

// Налаштування CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Конфігурація JWT аутентифікації
var jwtSecretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") 
    ?? throw new InvalidOperationException("JWT_SECRET_KEY не налаштовано в змінних середовища");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero,
            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization();

// Реєстрація сервісу шифрування
builder.Services.AddScoped<IEncryptionService, EncryptionService.Services.EncryptionService>();

// Конфігурація gRPC
builder.Services.AddGrpc(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.MaxReceiveMessageSize = 4 * 1024 * 1024; // 4MB
    options.MaxSendMessageSize = 4 * 1024 * 1024; // 4MB
});

// Конфігурація MassTransit з RabbitMQ (для інтеграції з іншими сервісами)
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitConfig = builder.Configuration.GetSection("RabbitMq");
        var host = rabbitConfig["Host"] ?? "rabbitmq://localhost";
        var username = rabbitConfig["Username"] ?? "guest";
        var password = rabbitConfig["Password"] ?? "guest";

        cfg.Host(host, h =>
        {
            h.Username(username);
            h.Password(password);
        });

        // Налаштування повторних спроб
        cfg.UseMessageRetry(r => r.Intervals(
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(15)
        ));

        cfg.ConfigureEndpoints(context);
    });
});

// Health Checks
builder.Services.AddHealthChecks()
    .AddCheck("encryption-key", () =>
    {
        var key = builder.Configuration["Encryption:Key"];
        return string.IsNullOrEmpty(key) 
            ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("Ключ шифрування не налаштовано")
            : Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Ключ шифрування налаштовано");
    });

// Swagger для документації API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Encryption Service API", Version = "v1" });
    
    // Додаємо JWT аутентифікацію до Swagger
    c.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Введіть JWT токен у форматі: Bearer {токен}"
    });
    
    c.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new()
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Логування
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Налаштування Kestrel для підтримки HTTP/1.1 та HTTP/2
builder.WebHost.ConfigureKestrel(options =>
{
    // HTTP endpoint для REST API
    options.ListenLocalhost(5004, o => o.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1);
    
    // HTTPS endpoint для REST API та gRPC
    options.ListenLocalhost(7104, o => 
    {
        o.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
        o.UseHttps();
    });
});

var app = builder.Build();

// Перевірка конфігурації ключа шифрування при старті
var encryptionKey = app.Configuration["Encryption:Key"];
if (string.IsNullOrEmpty(encryptionKey))
{
    app.Logger.LogWarning("УВАГА: Ключ шифрування не налаштовано. Використовується стандартний ключ.");
}

// Конфігурація middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Encryption Service V1");
        c.RoutePrefix = "swagger";
    });
}

// Middleware для обробки помилок
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Налаштування CORS
app.UseCors("AllowAll");

// Маршрутизація та аутентифікація
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Health checks endpoint
app.MapHealthChecks("/api/health");

// Контролери REST API
app.MapControllers();

// gRPC сервіси
app.MapGrpcService<EncryptionGrpcService>();

// Fallback для невідомих маршрутів
app.MapFallback(() => Results.NotFound(new { message = "Endpoint не знайдено" }));

app.Logger.LogInformation("Encryption Service запущено на портах: HTTP: 5004, HTTPS: 7104");
app.Logger.LogInformation("gRPC сервіс доступний на HTTPS: 7104");

// Запуск програми
app.Run();