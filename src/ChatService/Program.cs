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

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ChatDbContext>(options =>
    options.UseMySQL(builder.Configuration.GetConnectionString("ChatDatabase") ??
        throw new InvalidOperationException("Connection string 'ChatDatabase' not found.")));

// ������� CORS-������
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ������ �������������� �� ������������� JWT Bearer ����
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

builder.Services.AddHttpContextAccessor();

// �������� ��������
builder.Services.AddTransient<InternalAuthHandler>();

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

builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("rabbitmq://localhost", h =>
        {
            h.Username("guest");
            h.Password("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");
        });
        cfg.ConfigureEndpoints(context);
    });
});

// ��������� ����������
builder.Services.AddScoped<IChatRoomRepository, ChatRoomRepository>();
builder.Services.AddScoped<IFolderRepository, FolderRepository>();

// ��������� ������
builder.Services.AddScoped<IChatService, ChatService.Services.ChatService>();
builder.Services.AddScoped<IFolderService, FolderService>();

// ��������� ������ �����������
builder.Services.AddScoped<IChatAuthorizationService, ChatAuthorizationService>();

// ��������� ������ ��������� �������
builder.Services.AddScoped<IPermissionService<ChatPermission>, ChatPermissionService>();


builder.Services.AddScoped<IEventPublisher, OutboxEventPublisher>();
builder.Services.AddScoped<CreateChatRoomCommandConsumer>();
builder.Services.AddScoped<NotifyMessageServiceCommandConsumer>();
builder.Services.AddScoped<CompensateChatCreationCommandConsumer>();

builder.Services.AddHostedService<OutboxProcessorService>();
builder.Services.AddHostedService<OutboxCleanupService>();

builder.Services.AddGrpc();

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

app.MapGrpcService<ChatAuthorizationGrpcService>();

app.UseCors("AllowAll");

// ����������� ������������ ������� (��� ��������)
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

// ������������ middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseRouting();

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
