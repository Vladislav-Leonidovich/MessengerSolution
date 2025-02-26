using MassTransit;
using MessageService.Data;
using MessageService.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ������������ MassTransit � RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("rabbitmq://localhost", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });
    });
});

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("MessageDatabase")
    ?? "Server=localhost;Database=messagedb;User=root;Password=root;";

builder.Services.AddDbContext<MessageDbContext>(options =>
    options.UseMySQL(connectionString));

builder.Services.AddControllers();

// �������� ��� ����� ��� ������ � �������������
builder.Services.AddScoped<IMessageService, MessageService.Services.MessageService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ����������� ������� ���� ����� (������ ��� ��������)
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

// ��������� middleware
app.UseRouting();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
