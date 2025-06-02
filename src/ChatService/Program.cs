using System.IdentityModel.Tokens.Jwt;
using System.Text;
using ChatService.Authorization;
using ChatService.Data;
using ChatService.Middleware;
using ChatService.Repositories.Interfaces;
using ChatService.Repositories;
using ChatService.Services;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Shared.Authorization.Permissions;
using Shared.Authorization;
using ChatService.Services.Interfaces;
using ChatService.BackgroundServices;
using ChatService.Sagas.ChatCreation.Consumers;
using ChatService.Configuration;
using ChatService.Sagas.ChatCreation;
using ChatService.Mappers.Interfaces;
using ChatService.Mappers;
using ChatService.Consumers.ChatOperations;
using static Org.BouncyCastle.Math.EC.ECCurve;
using MySql.Data.MySqlClient;
using MessageService.Services;
using Shared.Interceptors;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ChatDbContext>(options =>
    options.UseMySQL(builder.Configuration.GetConnectionString("ChatDatabase") ??
        throw new InvalidOperationException("Connection string 'ChatDatabase' not found.")));

builder.Services.AddGrpc(options => {
    options.Interceptors.Add<ServerAuthInterceptor>();
});

builder.Services.AddScoped<ServerAuthInterceptor>();

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

builder.Services.AddAuthorization();
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ChatOperationStartCommandConsumer>();
    x.AddConsumer<ChatOperationProgressCommandConsumer>();
    x.AddConsumer<ChatOperationCompleteCommandConsumer>();
    x.AddConsumer<ChatOperationFailCommandConsumer>();
    x.AddConsumer<ChatOperationCompensateCommandConsumer>();
    x.AddConsumer<CreateChatRoomCommandConsumer>();
    x.AddConsumer<NotifyMessageServiceCommandConsumer>();
    x.AddConsumer<CompleteChatCreationCommandConsumer>();
    x.AddConsumer<CompensateChatCreationCommandConsumer>();

    x.ConfigureChatOperationConsumers();
    x.AddSagaStateMachine<ChatCreationSagaStateMachine, ChatCreationSagaState>()
        .EntityFrameworkRepository(r =>
        {
            r.ConcurrencyMode = ConcurrencyMode.Pessimistic;
            r.AddDbContext<DbContext, ChatDbContext>();
        });
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("rabbitmq://localhost", h =>
        {
            h.Username("guest");
            h.Password("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");
        });
        cfg.ReceiveEndpoint("chat-create-command", e =>
        {
            e.ConfigureConsumer<CreateChatRoomCommandConsumer>(context);
            e.UseMessageRetry(r => r.Intervals(
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(15)
            ));
        });

        cfg.ReceiveEndpoint("chat-notify-message-service", e =>
        {
            e.ConfigureConsumer<NotifyMessageServiceCommandConsumer>(context);
            e.UseMessageRetry(r => r.Intervals(
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(15)
            ));
        });

        cfg.ReceiveEndpoint("chat-compensate-creation", e =>
        {
            e.ConfigureConsumer<CompensateChatCreationCommandConsumer>(context);
            e.UseMessageRetry(r => r.Intervals(
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(15)
            ));
        });

        cfg.ReceiveEndpoint("chat-complete-creation", e =>
        {
            e.ConfigureConsumer<CompleteChatCreationCommandConsumer>(context);
        });

        // Конфігуруємо endpoint'и для ChatOperation
        context.ConfigureChatOperationEndpoints(cfg);

        // Конфігуруємо endpoint для саги
        cfg.ReceiveEndpoint("chat-creation-saga", e =>
        {
            e.ConfigureSaga<ChatCreationSagaState>(context);
        });
        cfg.ConfigureEndpoints(context);
    });
});

// Реєстрація репозиторіїв
builder.Services.AddScoped<IChatRoomRepository, ChatRoomRepository>();
builder.Services.AddScoped<IFolderRepository, FolderRepository>();

// Реєстрація сервісів
builder.Services.AddScoped<IChatService, ChatService.Services.ChatService>();
builder.Services.AddScoped<IFolderService, FolderService>();

// Реєстрація сервісу авторизації
builder.Services.AddScoped<IChatAuthorizationService, ChatAuthorizationService>();

// Реєстрація сервісу детальних дозволів
builder.Services.AddScoped<IPermissionService<ChatPermission>, ChatPermissionService>();
builder.Services.AddScoped<ITokenService, TokenService>();

builder.Services.AddScoped<IEventPublisher, OutboxEventPublisher>();
builder.Services.AddScoped<CreateChatRoomCommandConsumer>();
builder.Services.AddScoped<NotifyMessageServiceCommandConsumer>();
builder.Services.AddScoped<CompensateChatCreationCommandConsumer>();
builder.Services.AddScoped<CompleteChatCreationCommandConsumer>();
builder.Services.AddScoped<IChatOperationService, ChatOperationService>();

builder.Services.AddSingleton<IMapperFactory, MapperFactory>();
builder.Services.AddScoped<IEntityMapper<ChatService.Models.GroupChatRoom, Shared.DTOs.Chat.GroupChatRoomDto>, GroupChatRoomMapper>();
builder.Services.AddScoped<IEntityMapper<ChatService.Models.GroupChatMember, Shared.DTOs.Chat.GroupChatMemberDto>, GroupChatMemberMapper>();
builder.Services.AddScoped<IEntityMapper<ChatService.Models.PrivateChatRoom, Shared.DTOs.Chat.ChatRoomDto>, PrivateChatRoomMapper>();
builder.Services.AddScoped<IEntityMapper<ChatService.Models.Folder, Shared.DTOs.Folder.FolderDto>, FolderMapper>();
builder.Services.AddScoped<IEntityMapper<Shared.Protos.MessageData, Shared.DTOs.Message.MessageDto>, MessageMapper>();
builder.Services.AddScoped<IEntityMapper<Shared.Protos.UserData, Shared.DTOs.Identity.UserDto>, UserMapper>();

builder.Services.AddHostedService<OutboxProcessorService>();
builder.Services.AddHostedService<OutboxCleanupService>();
builder.Services.AddChatOperationServices();
builder.Services.AddGrpcClients(builder.Configuration);

builder.Services.AddControllers();

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

app.MapGrpcService<ChatGrpcService>();

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
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseRouting();

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
