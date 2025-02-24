using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Yarp.ReverseProxy;

// Створення Builder для додатку
var builder = WebApplication.CreateBuilder(args);

// Додаємо CORS-політику
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorClient", policy =>
    {
        // У режимі розробки часто дозволяють усе, але в продакшені краще обмежити Origins
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Реєстрація YARP (Reverse Proxy) та завантаження конфігурації з секції "ReverseProxy" з appsettings.json
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

if (builder.Environment.IsDevelopment())
{
    // Реєструємо іменований HttpClient для YARP і налаштовуємо його так, щоб у режимі розробки приймав будь-які сертифікати
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

// Додаємо підтримку контролерів (наприклад, для health-check)
builder.Services.AddControllers();

// Створюємо додаток
var app = builder.Build();

// Застосовуємо CORS-політику для всіх запитів
app.UseCors("AllowBlazorClient");

app.UseAuthentication();

app.UseAuthorization();

// Налаштування маршрутизації для контролерів
app.MapControllers();

// Налаштування YARP: цей middleware буде пересилати запити до інших мікросервісів згідно з конфігурацією
app.MapReverseProxy();

// Запуск додатку
app.Run();