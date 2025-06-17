using ChatService.Configuration;
using IdentityService.Services;
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

    // ������������ ���� �����
    builder.Services.AddDatabase(builder.Configuration);

    // ������������ ������� �������
    builder.Services.AddExceptionHandling();

    // ������������ �������������� �� �����������
    builder.Services.AddJwtAuthentication(builder.Configuration);
    builder.Services.AddCustomAuthorization();

    // ��������� ����������
    builder.Services.AddRepositories();

    // ��������� �����-������
    builder.Services.AddBusinessServices();

    builder.Services.AddGrpcInterceptors();

    builder.Services.AddGrpcServers(builder.Environment);

    builder.Services.AddGrpcServiceOptions<IdentityGrpcService>(options =>
    {
        options.MaxReceiveMessageSize = 16 * 1024 * 1024;
        options.MaxSendMessageSize = 16 * 1024 * 1024;
    });

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

    // ����������� ������������ ������� (����� � Development)
    if (app.Environment.IsDevelopment())
    {
        app.UseDatabaseMigration();
    }

    // ������������ middleware ������� (����� � Development)
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
        app.UseDatabaseMigration();

        // ������ Swagger ����� � Development
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Identity Service API v1"));
    }
    else
    {
        // � Production ����� ��������������� ���������� �������� �������
        app.UseExceptionHandler("/error");
    }

    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();

    // ����� ����������
    app.MapControllers();

    // gRPC ������ � interceptor'��
    app.MapGrpcService<IdentityGrpcService>()
        .RequireAuthorization(); // �������� ����������� ��� gRPC

    // Health check endpoint
    app.MapGet("/health", () => Results.Ok(new
    {
        status = "healthy",
        service = "IdentityService",
        timestamp = DateTime.UtcNow,
        environment = app.Environment.EnvironmentName
    }));

    // ��������� ���
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("IdentityService ����������� � ����������: {Environment}", app.Environment.EnvironmentName);
    logger.LogInformation("���� �����: {ConnectionString}",
        builder.Configuration.GetConnectionString("IdentityDatabase")?.Replace("Password=", "Password=***"));

    app.Run();
}
catch (Exception ex)
{
    // ��������� ��������� ������� �� ��� �������
    var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<Program>();
    logger.LogCritical(ex, "�������� ������� �� ��� ������� IdentityService");
    throw;
}

// �������� ����� Program ��� ����������
public partial class Program { }