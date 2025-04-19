using System.IdentityModel.Tokens.Jwt;
using System.Text;
using ChatService.Consumers;
using MassTransit;
using MessageService.Consumers;
using MessageService.Data;
using MessageService.Hubs;
using MessageService.Repositories.Interfaces;
using MessageService.Repositories;
using MessageService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MessageService.Authorization;
using MessageService.Middleware;
using MessageService.Services.Interfaces;
using Shared.Contracts;
using Shared.Interceptors;
using Shared.Protos;
using MessageService.BackgroundServices;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<MessageDbContext>(options =>
    options.UseMySQL(builder.Configuration.GetConnectionString("MessageDatabase")));

builder.Services.AddScoped<IMessageService, MessageService.Services.MessageService>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IMessageAuthorizationService, MessageAuthorizationService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IEventPublisher, OutboxEventPublisher>();
builder.Services.AddHostedService<OutboxProcessorService>();

builder.Services.AddSingleton<IEncryptionGrpcClient, EncryptionGrpcClient>();
builder.Services.AddSingleton<IChatGrpcClient, ChatGrpcClient>();
builder.Services.AddMemoryCache();

builder.Services.AddGrpcClient<EncryptionGrpcService.EncryptionGrpcServiceClient>(o =>
{
    o.Address = new Uri(builder.Configuration["Services:EncryptionService:GrpcUrl"]);
})
.AddInterceptor(provider =>
{
    var tokenService = provider.GetRequiredService<ITokenService>();
    var logger = provider.GetRequiredService<ILogger<AuthGrpcInterceptor>>();

    // Создаем функцию-провайдер токена
    Func<Task<string>> tokenProvider = async () => await tokenService.GetTokenAsync();

    // Создаем и возвращаем перехватчик
    return new AuthGrpcInterceptor(tokenProvider, logger);
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new SocketsHttpHandler
    {
        EnableMultipleHttp2Connections = true,
        KeepAlivePingDelay = TimeSpan.FromSeconds(60),
        KeepAlivePingTimeout = TimeSpan.FromSeconds(30)
    };

    // Настройка проверки SSL через SslOptions
    handler.SslOptions = new System.Net.Security.SslClientAuthenticationOptions
    {
        // Отключает проверку сертификатов (только для разработки!)
        RemoteCertificateValidationCallback = (sender, certificate, chain, errors) => true
    };

    return handler;
});

builder.Services.AddHttpContextAccessor();

// Налаштування MassTransit з RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<MessageCreatedEventConsumer>();
    x.AddConsumer<ChatDeletedEventConsumer>();
    x.AddConsumer<MessageUpdatedEventConsumer>();
    x.AddConsumer<ChatAccessChangedEventConsumer>();
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("rabbitmq://localhost", h =>
        {
            h.Username("guest");
            h.Password("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");
        });

        cfg.ReceiveEndpoint("message-events-queue", e =>
        {
            e.ConfigureConsumer<MessageCreatedEventConsumer>(context);
            e.ConfigureConsumer<MessageUpdatedEventConsumer>(context);
        });

        cfg.ReceiveEndpoint("chat-events-queue", e =>
        {
            e.ConfigureConsumer<ChatDeletedEventConsumer>(context);
            e.ConfigureConsumer<ChatAccessChangedEventConsumer>(context);
        });
    });
});

builder.Services.AddSignalR();

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

app.UseMiddleware<ExceptionHandlingMiddleware>();

// Настройка middleware
app.UseRouting();

app.MapHub<MessageHub>("/messageHub");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
