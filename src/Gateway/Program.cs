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
    options.AddPolicy("AllowBlazorClient", policy =>
    {
        // � ����� �������� ����� ���������� ���, ��� � ��������� ����� �������� Origins
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
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

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSecretKey = "9J*4&fR+T2s!@lK8nQvL1$pOiWzBx3#6^jCe7YmH_dVtX5?AcN0b%MuSw~ErG235";
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

// ������ �������� ���������� (���������, ��� health-check)
builder.Services.AddControllers();

// ��������� �������
var app = builder.Build();

// ����������� CORS-������� ��� ��� ������
app.UseCors("AllowBlazorClient");

app.UseAuthentication();

app.UseAuthorization();

// ������������ ������������� ��� ����������
app.MapControllers();

// ������������ YARP: ��� middleware ���� ���������� ������ �� ����� ���������� ����� � �������������
app.MapReverseProxy();

// ������ �������
app.Run();