using ChatService.Configuration;
using IdentityService.Services;
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

    // Налаштування бази даних
    builder.Services.AddDatabase(builder.Configuration);

    // Налаштування обробки помилок
    builder.Services.AddExceptionHandling();

    // Налаштування автентифікації та авторизації
    builder.Services.AddJwtAuthentication(builder.Configuration);
    builder.Services.AddCustomAuthorization();

    // Реєстрація репозиторіїв
    builder.Services.AddRepositories();

    // Реєстрація бізнес-сервісів
    builder.Services.AddBusinessServices();

    builder.Services.AddGrpcInterceptors();

    builder.Services.AddGrpcServers(builder.Environment);

    builder.Services.AddGrpcServiceOptions<IdentityGrpcService>(options =>
    {
        options.MaxReceiveMessageSize = 16 * 1024 * 1024;
        options.MaxSendMessageSize = 16 * 1024 * 1024;
    });

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

    // Автоматичне застосування міграцій (тільки в Development)
    if (app.Environment.IsDevelopment())
    {
        app.UseDatabaseMigration();
    }

    // Налаштування middleware помилок (краще в Development)
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
        app.UseDatabaseMigration();

        // Додаємо Swagger тільки в Development
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Identity Service API v1"));
    }
    else
    {
        // В Production можна використовувати глобальний обробник помилок
        app.UseExceptionHandler("/error");
    }

    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();

    // Мапінг контролерів
    app.MapControllers();

    // gRPC сервіси з interceptor'ом
    app.MapGrpcService<IdentityGrpcService>()
        .RequireAuthorization(); // Вимагаємо авторизацію для gRPC

    // Health check endpoint
    app.MapGet("/health", () => Results.Ok(new
    {
        status = "healthy",
        service = "IdentityService",
        timestamp = DateTime.UtcNow,
        environment = app.Environment.EnvironmentName
    }));

    // Стартовий лог
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("IdentityService запускається в середовищі: {Environment}", app.Environment.EnvironmentName);
    logger.LogInformation("База даних: {ConnectionString}",
        builder.Configuration.GetConnectionString("IdentityDatabase")?.Replace("Password=", "Password=***"));

    app.Run();
}
catch (Exception ex)
{
    // Логування критичних помилок під час запуску
    var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<Program>();
    logger.LogCritical(ex, "Критична помилка під час запуску IdentityService");
    throw;
}

// Часткова класс Program для тестування
public partial class Program { }