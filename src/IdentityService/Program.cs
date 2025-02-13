using System.Text;
using IdentityService.Data;
using IdentityService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Читання рядка підключення з appsettings.json
var connectionString = builder.Configuration.GetConnectionString("IdentityDatabase")
    ?? "Server=localhost;Database=IdentityDb;User=root;Password=yourpassword;";

builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseMySQL(connectionString));

builder.Services.AddControllers();

// Реєструємо наш сервіс аутентифікації, який реалізує IAuthService
builder.Services.AddScoped<IAuthService, AuthService>();

// Конфігурація JWT аутентифікації
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] ?? "SuperSecretKey12345"; // Ключ можна зберігати в секції «Jwt» у appsettings.json
var key = Encoding.UTF8.GetBytes(jwtSecretKey);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero // Прибираємо відставання за часом
    };
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    dbContext.Database.Migrate();
}


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Конфигурация middleware
app.UseRouting();

app.UseAuthentication();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
