using System.IdentityModel.Tokens.Jwt;
using System.Text;
using ChatService.Data;
using ChatService.Services;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();

// Додайте CORS-сервіси
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Додаємо аутентифікацію із використанням JWT Bearer схем
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
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("rabbitmq://localhost", h =>
        {
            h.Username("guest");
            h.Password("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");
        });
    });
});

// Add services to the container.
// Зчитування рядка підключення з appsettings.json
var connectionString = builder.Configuration.GetConnectionString("ChatDatabase")
    ?? "Server=localhost;Database=chatdb;User=root;Password=root;";

builder.Services.AddDbContext<ChatDbContext>(options =>
    options.UseMySQL(connectionString));

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor(); // Для доступу до HttpContext у сервісах

// Реєстрація сервісу для роботи з чатами
builder.Services.AddScoped<IChatService, ChatService.Services.ChatService>();
builder.Services.AddScoped<IFolderService, FolderService>();

builder.Services.AddHttpClient("IdentityClient", client =>
{
    client.BaseAddress = new Uri("https://localhost:7101/");
})
.AddHttpMessageHandler<InternalAuthHandler>();

builder.Services.AddHttpClient("MessageClient", client =>
{
    client.BaseAddress = new Uri("https://localhost:7103/");
})
.AddHttpMessageHandler<InternalAuthHandler>();

// Реєструємо обробник
builder.Services.AddTransient<InternalAuthHandler>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

var app = builder.Build();

app.UseCors("AllowAll");

// Автоматичне застосування міграцій (для розробки)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
    dbContext.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Налаштування middleware
app.UseRouting();

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
