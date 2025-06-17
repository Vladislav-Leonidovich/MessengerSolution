using MessageService.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Interceptors;
using Shared.Protos;
using MessageService.Configuration;
using MessageService.Hubs;
using MessageService.Data;
using MessageService.BackgroundServices;
using Shared.Outbox;

var builder = WebApplication.CreateBuilder(args);
try
{
    // === ���Բ����ֲ� ���²Ѳ� ===

    // ����� ASP.NET Core ������
    builder.Services.AddControllers();

    // ������������ ������� �������
    builder.Services.AddExceptionHandling();

    builder.Services.AddHttpClient();

    // ������������ ���� �����
    builder.Services.AddDatabase(builder.Configuration);

    // ������������ �������������� �� �����������
    builder.Services.AddJwtAuthentication(builder.Configuration);
    builder.Services.AddCustomAuthorization();

    // ��������� ����������
    builder.Services.AddRepositories();

    // ��������� �����-������
    builder.Services.AddBusinessServices();

    // ��������� ������ �����������
    builder.Services.AddAuthorizationServices();
    // gRPC �볺���
    builder.Services.AddGrpcClients(builder.Configuration);

    builder.Services.AddGrpcInterceptors();

    builder.Services.AddGrpcServers(builder.Environment);

    builder.Services.AddGrpcServiceOptions<MessageService.Services.MessageGrpcService>(options =>
    {
        options.MaxReceiveMessageSize = 16 * 1024 * 1024;
        options.MaxSendMessageSize = 16 * 1024 * 1024;
    });

    builder.Services.AddGrpcClientWrappers();
    // ������ HTTP �볺���
    builder.Services.AddServiceHttpClients(builder.Configuration, builder.Environment);

    // MassTransit
    builder.Services.AddMassTransitConfiguration(builder.Configuration);

    // ChatOperation ������
    builder.Services.AddMessageOperationServices();

    // ����� ������
    //builder.Services.AddBackgroundServices();

    // Outbox �������
    //builder.Services.AddOutboxProcessing<MessageDbContext, MessageOutboxProcessorService>();

    // Swagger ������������
    builder.Services.AddSwaggerDocumentation();

    // CORS
    builder.Services.AddCors();

    builder.Services.AddSignalR();

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

    app.MapHub<MessageHub>("/messageHub");

    // gRPC ������ � interceptor'��
    app.MapGrpcService<MessageService.Services.MessageGrpcService>()
        .RequireAuthorization(); // �������� ����������� ��� gRPC

    // Health check endpoint
    app.MapGet("/health", () => Results.Ok(new
    {
        status = "healthy",
        service = "MessageService",
        timestamp = DateTime.UtcNow,
        environment = app.Environment.EnvironmentName
    }));

    // ��������� ���
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("MessageService ����������� � ����������: {Environment}", app.Environment.EnvironmentName);
    logger.LogInformation("���� �����: {ConnectionString}",
        builder.Configuration.GetConnectionString("MessageDatabase")?.Replace("Password=", "Password=***"));

    app.Run();
}
catch (Exception ex)
{
    // ��������� ��������� ������� �� ��� �������
    var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<Program>();
    logger.LogCritical(ex, "�������� ������� �� ��� ������� MessageService");
    throw;
}

// �������� ����� Program ��� ����������
public partial class Program { }