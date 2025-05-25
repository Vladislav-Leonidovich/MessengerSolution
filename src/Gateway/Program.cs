using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Gateway.Extensions;
using Gateway.HealthChecks;
using Gateway.Middleware;
using Gateway.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Yarp.ReverseProxy;

// ��������� Builder ��� �������
var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// ������ �������� ���������� (���������, ��� health-check)
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHostedService<ConfigurationMonitoringService>();

// ������ CORS-�������
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        // � ����� �������� ����� ���������� ���, ��� � ��������� ����� �������� Origins
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSecretKey = builder.Configuration["JWT_SECRET_KEY"];
        if (string.IsNullOrEmpty(jwtSecretKey))
        {
            throw new InvalidOperationException("JWT Secret Key is not configured");
        }
        var key = Encoding.UTF8.GetBytes(jwtSecretKey);

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddHealthChecks()
    .AddCheck<IdentityServiceHealthCheck>("identity_service", tags: new[] { "services" })
    .AddCheck<ChatServiceHealthCheck>("chat_service", tags: new[] { "services" })
    .AddCheck<MessageServiceHealthCheck>("message_service", tags: new[] { "services" })
    .AddCheck<EncryptionServiceHealthCheck>("encryption_service", tags: new[] { "services" });

// ��������� YARP (Reverse Proxy) �� ������������ ������������ � ������ "ReverseProxy" � appsettings.json
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddProxyResiliencePolicies();


if (builder.Environment.IsDevelopment())
{
    // �������� ���������� HttpClient ��� YARP � ����������� ���� ���, ��� � ����� �������� ������� ����-�� �����������
    builder.Services.AddHttpClient("ReverseProxy")
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            return handler;
        });
}

// ��������� �������
var app = builder.Build();

// ����������� CORS-������� ��� ��� ������
app.UseCors("AllowAll");

app.UseHttpsRedirection();

app.UseMiddleware<ProxyErrorHandlingMiddleware>();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

// ������������ ������������� ��� ����������
app.MapControllers();

// ������������ YARP: ��� middleware ���� ���������� ������ �� ����� ���������� ����� � �������������
app.MapReverseProxy();

app.MapHealthChecks("/health");

// ������ �������
app.Run();