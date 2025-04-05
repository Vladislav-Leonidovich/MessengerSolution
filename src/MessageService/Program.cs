using System.IdentityModel.Tokens.Jwt;
using System.Text;
using ChatService.Consumers;
using MassTransit;
using MessageService.Consumers;
using MessageService.Data;
using MessageService.Hubs;
using MessageService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();

// Налаштування MassTransit з RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<MessageCreatedEventConsumer>();
    x.AddConsumer<ChatDeletedEventConsumer>();
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("rabbitmq://localhost", h =>
        {
            h.Username("guest");
            h.Password("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");
        });

        cfg.ReceiveEndpoint("message-service-message-created", e =>
        {
            // Спочатку вказуємо властивості черги
            e.Durable = true;       // Черга переживе перезапуск RabbitMQ
            e.AutoDelete = false;   // Черга не видалиться сама при відключенні
            e.PrefetchCount = 10;

            // Потім підключаємо споживача
            e.ConfigureConsumer<MessageCreatedEventConsumer>(context);
        });

        cfg.ReceiveEndpoint("message-service-chat-deleted", e =>
        {
            // Спочатку вказуємо властивості черги
            e.Durable = true;       // Черга переживе перезапуск RabbitMQ
            e.AutoDelete = false;   // Черга не видалиться сама при відключенні
            e.PrefetchCount = 10;

            // Потім підключаємо споживача
            e.ConfigureConsumer<ChatDeletedEventConsumer>(context);
        });

        cfg.UseMessageRetry(r => r.Incremental(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5)));
    });
});

builder.Services.AddSignalR();

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("MessageDatabase")
    ?? "Server=localhost;Database=messagedb;User=root;Password=root;";

builder.Services.AddDbContext<MessageDbContext>(options =>
    options.UseMySQL(connectionString));

builder.Services.AddControllers();

// Реєструємо наш сервіс для роботи з повідомленнями
builder.Services.AddScoped<IMessageService, MessageService.Services.MessageService>();

builder.Services.AddHttpClient("EncryptionClient", client =>
{
    client.BaseAddress = new Uri("https://localhost:7104/");
})
.AddHttpMessageHandler<InternalAuthHandler>();
builder.Services.AddHttpClient("ChatClient", client =>
{
    client.BaseAddress = new Uri("https://localhost:7102/");
})
.AddHttpMessageHandler<InternalAuthHandler>();
builder.Services.AddHttpClient("IdentityClient", client =>
{
    client.BaseAddress = new Uri("https://localhost:7101/");
})
.AddHttpMessageHandler<InternalAuthHandler>();

// Реєструємо обробник
builder.Services.AddTransient<InternalAuthHandler>();

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

// Автоматична міграція бази даних (зручно для розробки)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<MessageDbContext>();
    dbContext.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Настройка middleware
app.UseRouting();

app.MapHub<MessageHub>("/messageHub");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
