using System.IdentityModel.Tokens.Jwt;
using System.Text;
using MassTransit;
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
using MessageService.Sagas.MessageDelivery.Consumers;
using MessageService.Sagas.MessageDelivery;
using MessageService.Authorization.ChatService.Authorization;
using Shared.Authorization.Permissions;
using Shared.Authorization;
using MessageService.Sagas.DeleteAllMessages.Consumers;
using MessageService.Sagas.DeleteAllMessages;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<MessageDbContext>(options =>
    options.UseMySQL(builder.Configuration.GetConnectionString("MessageDatabase")));

builder.Services.AddDbContext<MessageDeliverySagaDbContext>(options =>
    options.UseMySQL(builder.Configuration.GetConnectionString("SagaDatabase")));

builder.Services.AddScoped<IMessageService, MessageService.Services.MessageService>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IMessageAuthorizationService, MessageAuthorizationService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IEventPublisher, OutboxEventPublisher>();
builder.Services.AddScoped<IPermissionService<MessagePermission>, MessagePermissionService>();
builder.Services.AddHostedService<OutboxProcessorService>();
builder.Services.AddHostedService<OutboxCleanupService>();
builder.Services.AddScoped<SaveMessageCommandConsumer>();
builder.Services.AddScoped<PublishMessageCommandConsumer>();
builder.Services.AddScoped<CheckDeliveryStatusCommandConsumer>();
builder.Services.AddScoped<MessageDeliveredToUserEventConsumer>();
builder.Services.AddScoped<DeleteChatMessagesCommandConsumer>();
builder.Services.AddScoped<SendChatNotificationCommandConsumer>();

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
    // Реєстрація консьюмерів

    // Реєстрація споживачів для саги видалення повідомлень
    x.AddConsumer<DeleteChatMessagesCommandConsumer>();
    x.AddConsumer<SendChatNotificationCommandConsumer>();

    // Реєстрація споживачів для саги доставки повідомлень
    x.AddConsumer<SaveMessageCommandConsumer>();
    x.AddConsumer<PublishMessageCommandConsumer>();
    x.AddConsumer<CheckDeliveryStatusCommandConsumer>();
    x.AddConsumer<MessageDeliveredToUserEventConsumer>();

    // Реєстрація консьюмерів для Outbox

    x.AddSagaStateMachine<MessageDeliverySagaStateMachine, MessageDeliverySagaState>()
        .EntityFrameworkRepository(r =>
        {
            r.ExistingDbContext<MessageDeliverySagaDbContext>();
            r.ConcurrencyMode = ConcurrencyMode.Optimistic;
        });

    x.AddSagaStateMachine<DeleteAllMessagesSagaStateMachine, DeleteAllMessagesSagaState>()
         .EntityFrameworkRepository(r =>
         {
             r.ExistingDbContext<MessageDbContext>();
             r.ConcurrencyMode = ConcurrencyMode.Optimistic;
         });

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMq:Host"], h =>
        {
            h.Username(builder.Configuration["RabbitMq:Username"]);
            h.Password(builder.Configuration["RabbitMq:Password"]);
        });

        cfg.ReceiveEndpoint("delete-chat-messages", e =>
        {
            e.ConfigureConsumer<DeleteChatMessagesCommandConsumer>(context);
        });

        cfg.ReceiveEndpoint("send-chat-notification", e =>
        {
            e.ConfigureConsumer<SendChatNotificationCommandConsumer>(context);
        });

        cfg.ReceiveEndpoint("save-message", e =>
        {
            e.ConfigureConsumer<SaveMessageCommandConsumer>(context);
        });

        cfg.ReceiveEndpoint("publish-message", e =>
        {
            e.ConfigureConsumer<PublishMessageCommandConsumer>(context);
        });

        cfg.ReceiveEndpoint("check-delivery-status", e =>
        {
            e.ConfigureConsumer<CheckDeliveryStatusCommandConsumer>(context);
        });

        // Налаштування черг для саг
        cfg.ReceiveEndpoint("delete-all-messages-saga", e =>
        {
            e.ConfigureSaga<DeleteAllMessagesSagaState>(context);
        });

        cfg.ReceiveEndpoint("message-delivery-saga", e =>
        {
            e.ConfigureSaga<MessageDeliverySagaState>(context);
        });

        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddSignalR();

/*builder.Services.AddHttpClient("EncryptionClient", client =>
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
.AddHttpMessageHandler<InternalAuthHandler>();*/

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
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/messageHub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
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
// Міграції бази даних при запуску
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var messageContext = services.GetRequiredService<MessageDbContext>();
        messageContext.Database.Migrate();

        var sagaContext = services.GetRequiredService<MessageDeliverySagaDbContext>();
        sagaContext.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Помилка при міграції бази даних");
    }
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
