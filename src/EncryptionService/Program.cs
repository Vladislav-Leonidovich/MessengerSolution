using ChatService.Configuration;
using EncryptionService.Configuration;
using EncryptionService.Middleware;
using EncryptionService.Services;
using EncryptionService.Services.Interfaces;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

try
{
    // === КОНФІГУРАЦІЯ СЕРВІСІВ ===

    // Базові ASP.NET Core сервіси
    builder.Services.AddControllers();

    // Налаштування обробки помилок
    builder.Services.AddExceptionHandling();

    // Налаштування автентифікації та авторизації
    builder.Services.AddJwtAuthentication(builder.Configuration);
    builder.Services.AddCustomAuthorization();

    // Реєстрація бізнес-сервісів
    builder.Services.AddBusinessServices();

    builder.Services.AddGrpcInterceptors();

    builder.Services.AddGrpcServers(builder.Environment);

    builder.Services.AddGrpcServiceOptions<EncryptionGrpcService>(options =>
    {
        options.MaxReceiveMessageSize = 16 * 1024 * 1024;
        options.MaxSendMessageSize = 16 * 1024 * 1024;
    });

    // Додаємо HTTP клієнти
    builder.Services.AddServiceHttpClients(builder.Configuration, builder.Environment);

    // Swagger документація
    builder.Services.AddSwaggerDocumentation();

    // CORS
    builder.Services.AddCors();

    // Логування
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();
    builder.Logging.AddDebug();

    if (builder.Environment.IsDevelopment())
    {
        builder.Logging.SetMinimumLevel(LogLevel.Debug);
    }

    var app = builder.Build();

    // === КОНФІГУРАЦІЯ PIPELINE ===

    // Базовий middleware pipeline
    app.UseBasicMiddleware(app.Environment);

    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();

    // Мапінг контролерів
    app.MapControllers();

    // gRPC сервіси з interceptor'ом
    app.MapGrpcService<EncryptionGrpcService>()
        .RequireAuthorization();// Вимагаємо авторизацію для gRPC

    // Health check endpoint
    app.MapGet("/health", () => Results.Ok(new
    {
        status = "healthy",
        service = "EncryptionService",
        timestamp = DateTime.UtcNow,
        environment = app.Environment.EnvironmentName
    }));

    // Стартовий лог
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("ChatService запускається в середовищі: {Environment}", app.Environment.EnvironmentName);

    app.Run();
}
catch (Exception ex)
{
    // Логування критичних помилок під час запуску
    var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<Program>();
    logger.LogCritical(ex, "Критична помилка під час запуску EncryptionService");
    throw;
}

// Часткова класс Program для тестування
public partial class Program { }