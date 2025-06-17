using ChatService.Services;
using Microsoft.EntityFrameworkCore;
using ChatService.Configuration;
using Shared.Interceptors;
using Shared.Outbox;
using ChatService.Data;
using ChatService.BackgroundServices;
using ChatService.Hubs;

var builder = WebApplication.CreateBuilder(args);

try
{
    // === КОНФІГУРАЦІЯ СЕРВІСІВ ===

    // Базові ASP.NET Core сервіси
    builder.Services.AddControllers();

    // Налаштування обробки помилок
    builder.Services.AddExceptionHandling();

    // Налаштування бази даних
    builder.Services.AddDatabase(builder.Configuration);

    // Налаштування автентифікації та авторизації
    builder.Services.AddJwtAuthentication(builder.Configuration);
    builder.Services.AddCustomAuthorization();

    // Реєстрація репозиторіїв
    builder.Services.AddRepositories();

    // Реєстрація бізнес-сервісів
    builder.Services.AddBusinessServices();

    // Реєстрація mapper'ів
    builder.Services.AddMappers();

    // Реєстрація сервісів авторизації
    builder.Services.AddAuthorizationServices();

    // gRPC клієнти
    builder.Services.AddGrpcClients(builder.Configuration);

    builder.Services.AddGrpcInterceptors();

    builder.Services.AddGrpcServers(builder.Environment);

    builder.Services.AddGrpcServiceOptions<ChatGrpcService>(options =>
    {
        options.MaxReceiveMessageSize = 16 * 1024 * 1024;
        options.MaxSendMessageSize = 16 * 1024 * 1024;
    });

    builder.Services.AddGrpcClientWrappers();
    // Додаємо HTTP клієнти
    builder.Services.AddServiceHttpClients(builder.Configuration, builder.Environment);

    // MassTransit
    builder.Services.AddMassTransitConfiguration(builder.Configuration);

    builder.Services.AddSignalR();

    // ChatOperation сервіси
    builder.Services.AddChatOperationServices();

    // Outbox обробка
    //builder.Services.AddOutboxProcessing<ChatDbContext, ChatOutboxProcessorService>();

    // Фонові сервіси
    //builder.Services.AddBackgroundServices();

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

    // Автоматичне застосування міграцій (тільки в Development)
    if (app.Environment.IsDevelopment())
    {
        app.UseDatabaseMigration();
    }

    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();

    // Мапінг контролерів
    app.MapControllers();

    app.MapHub<ChatHub>("/chatHub");

    // gRPC сервіси з interceptor'ом
    app.MapGrpcService<ChatGrpcService>()
        .RequireAuthorization(); // Вимагаємо авторизацію для gRPC

    // Health check endpoint
    app.MapGet("/health", () => Results.Ok(new
    {
        status = "healthy",
        service = "ChatService",
        timestamp = DateTime.UtcNow,
        environment = app.Environment.EnvironmentName
    }));

    // Стартовий лог
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("ChatService запускається в середовищі: {Environment}", app.Environment.EnvironmentName);
    logger.LogInformation("База даних: {ConnectionString}",
        builder.Configuration.GetConnectionString("ChatDatabase")?.Replace("Password=", "Password=***"));

    app.Run();
}
catch (Exception ex)
{
    // Логування критичних помилок під час запуску
    var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<Program>();
    logger.LogCritical(ex, "Критична помилка під час запуску ChatService");
    throw;
}

// Часткова класс Program для тестування
public partial class Program { }