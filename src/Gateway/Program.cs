using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Yarp.ReverseProxy;

// ��������� Builder ��� �������
var builder = WebApplication.CreateBuilder(args);

// ������ CORS-�������
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        // � ����� �������� ����� ���������� ���, ��� � ���������� ����� �������� Origins
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

// ��������� YARP (Reverse Proxy) �� ������������ ������������ � ������ "ReverseProxy" � appsettings.json
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

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

// ������ �������� ���������� (���������, ��� health-check)
builder.Services.AddControllers();

// ��������� �������
var app = builder.Build();

// ����������� CORS-������� ��� ��� ������
app.UseCors("AllowAll");

app.UseAuthentication();

app.UseAuthorization();

// ������������ ������������� ��� ����������
app.MapControllers();

// ������������ YARP: ��� middleware ���� ���������� ������ �� ����� ���������� ����� � �������������
app.MapReverseProxy();

// ������ �������
app.Run();