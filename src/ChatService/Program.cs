using ChatService.Services;
using Microsoft.EntityFrameworkCore;
using ChatService.Configuration;
using Shared.Interceptors;

var builder = WebApplication.CreateBuilder(args);

try
{
    // === ���Բ����ֲ� ���²Ѳ� ===

    // ����� ASP.NET Core ������
    builder.Services.AddControllers();

    // ������������ ������� �������
    builder.Services.AddExceptionHandling();

    // ������������ ���� �����
    builder.Services.AddDatabase(builder.Configuration);

    // ������������ �������������� �� �����������
    builder.Services.AddJwtAuthentication(builder.Configuration);
    builder.Services.AddCustomAuthorization();

    // ��������� ����������
    builder.Services.AddRepositories();

    // ��������� �����-������
    builder.Services.AddBusinessServices();

    // ��������� mapper'��
    builder.Services.AddMappers();

    // ��������� ������ �����������
    builder.Services.AddAuthorizationServices();

    // HTTP �볺���
    builder.Services.AddHttpClients(builder.Configuration);

    // gRPC �볺���
    builder.Services.AddGrpcClients(builder.Configuration);

    // gRPC �������
    builder.Services.AddGrpc(options =>
    {
        // ������������ gRPC
        options.MaxReceiveMessageSize = 16 * 1024 * 1024; // 16MB
        options.MaxSendMessageSize = 16 * 1024 * 1024;    // 16MB
        options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    })
    .AddServiceOptions<ChatGrpcService>(options =>
    {
        options.MaxReceiveMessageSize = 16 * 1024 * 1024;
        options.MaxSendMessageSize = 16 * 1024 * 1024;
    });

    // ������ ��������� interceptor ��� gRPC
    builder.Services.AddScoped<ServerAuthInterceptor>();

    // MassTransit
    builder.Services.AddMassTransitConfiguration(builder.Configuration);

    // ChatOperation ������
    builder.Services.AddChatOperationServices();

    // ����� ������
    builder.Services.AddBackgroundServices();

    // Swagger ������������
    builder.Services.AddSwaggerDocumentation();

    // CORS
    builder.Services.AddCors();

    // ���������
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();
    builder.Logging.AddDebug();

    if (builder.Environment.IsDevelopment())
    {
        builder.Logging.SetMinimumLevel(LogLevel.Debug);
    }

    var app = builder.Build();

    // === ���Բ����ֲ� PIPELINE ===

    // ������� middleware pipeline
    app.UseBasicMiddleware(app.Environment);

    // ����������� ������������ ������� (����� � Development)
    if (app.Environment.IsDevelopment())
    {
        app.UseDatabaseMigration();
    }

    // ����� ����������
    app.MapControllers();

    // gRPC ������ � interceptor'��
    app.MapGrpcService<ChatGrpcService>()
        .RequireAuthorization(); // �������� ����������� ��� gRPC
    app.MapGrpcService<MessageGrpcService>()
        .RequireAuthorization(); // �������� ����������� ��� gRPC
    app.MapGrpcService<IdentityGrpcService>()
        .RequireAuthorization(); // �������� ����������� ��� gRPC

    // Health check endpoint
    app.MapGet("/health", () => Results.Ok(new
    {
        status = "healthy",
        service = "ChatService",
        timestamp = DateTime.UtcNow,
        environment = app.Environment.EnvironmentName
    }));

    // ��������� ���
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("ChatService ����������� � ����������: {Environment}", app.Environment.EnvironmentName);
    logger.LogInformation("���� �����: {ConnectionString}",
        builder.Configuration.GetConnectionString("ChatDatabase")?.Replace("Password=", "Password=***"));

    app.Run();
}
catch (Exception ex)
{
    // ��������� ��������� ������� �� ��� �������
    var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<Program>();
    logger.LogCritical(ex, "�������� ������� �� ��� ������� ChatService");
    throw;
}

// �������� ����� Program ��� ����������
public partial class Program { }