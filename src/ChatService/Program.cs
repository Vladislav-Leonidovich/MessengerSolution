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

    // gRPC �볺���
    builder.Services.AddGrpcClients(builder.Configuration);

    builder.Services.AddGrpcInterceptors();

    builder.Services.AddGrpcServers(builder.Environment);

    builder.Services.AddGrpcServiceOptions<ChatGrpcService>(options =>
    {
        options.MaxReceiveMessageSize = 16 * 1024 * 1024;
        options.MaxSendMessageSize = 16 * 1024 * 1024;
    });

    builder.Services.AddGrpcClientWrappers();
    // ������ HTTP �볺���
    builder.Services.AddServiceHttpClients(builder.Configuration, builder.Environment);

    // MassTransit
    builder.Services.AddMassTransitConfiguration(builder.Configuration);

    builder.Services.AddSignalR();

    // ChatOperation ������
    builder.Services.AddChatOperationServices();

    // Outbox �������
    //builder.Services.AddOutboxProcessing<ChatDbContext, ChatOutboxProcessorService>();

    // ����� ������
    //builder.Services.AddBackgroundServices();

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

    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();

    // ����� ����������
    app.MapControllers();

    app.MapHub<ChatHub>("/chatHub");

    // gRPC ������ � interceptor'��
    app.MapGrpcService<ChatGrpcService>()
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