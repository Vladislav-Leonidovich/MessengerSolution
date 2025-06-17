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
    // === ���Բ����ֲ� ���²Ѳ� ===

    // ����� ASP.NET Core ������
    builder.Services.AddControllers();

    // ������������ ������� �������
    builder.Services.AddExceptionHandling();

    // ������������ �������������� �� �����������
    builder.Services.AddJwtAuthentication(builder.Configuration);
    builder.Services.AddCustomAuthorization();

    // ��������� �����-������
    builder.Services.AddBusinessServices();

    builder.Services.AddGrpcInterceptors();

    builder.Services.AddGrpcServers(builder.Environment);

    builder.Services.AddGrpcServiceOptions<EncryptionGrpcService>(options =>
    {
        options.MaxReceiveMessageSize = 16 * 1024 * 1024;
        options.MaxSendMessageSize = 16 * 1024 * 1024;
    });

    // ������ HTTP �볺���
    builder.Services.AddServiceHttpClients(builder.Configuration, builder.Environment);

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

    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();

    // ����� ����������
    app.MapControllers();

    // gRPC ������ � interceptor'��
    app.MapGrpcService<EncryptionGrpcService>()
        .RequireAuthorization();// �������� ����������� ��� gRPC

    // Health check endpoint
    app.MapGet("/health", () => Results.Ok(new
    {
        status = "healthy",
        service = "EncryptionService",
        timestamp = DateTime.UtcNow,
        environment = app.Environment.EnvironmentName
    }));

    // ��������� ���
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("ChatService ����������� � ����������: {Environment}", app.Environment.EnvironmentName);

    app.Run();
}
catch (Exception ex)
{
    // ��������� ��������� ������� �� ��� �������
    var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<Program>();
    logger.LogCritical(ex, "�������� ������� �� ��� ������� EncryptionService");
    throw;
}

// �������� ����� Program ��� ����������
public partial class Program { }