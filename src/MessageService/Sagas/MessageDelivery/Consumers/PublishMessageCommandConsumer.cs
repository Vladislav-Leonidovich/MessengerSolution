using MassTransit;
using MessageService.Data;
using MessageService.Hubs;
using MessageService.Services.Interfaces;
using MessageServiceDTOs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts;
using Shared.Exceptions;
using Shared.Sagas;

namespace MessageService.Sagas.MessageDelivery.Consumers
{
    public class PublishMessageCommandConsumer : IConsumer<PublishMessageCommand>
    {
        private readonly IHubContext<MessageHub> _hubContext;
        private readonly IEncryptionGrpcClient _encryptionClient;
        private readonly ILogger<PublishMessageCommandConsumer> _logger;
        private readonly MessageDbContext _dbContext;

        public PublishMessageCommandConsumer(
            IHubContext<MessageHub> hubContext,
            IEncryptionGrpcClient encryptionClient,
            MessageDbContext dbContext,
            ILogger<PublishMessageCommandConsumer> logger)
        {
            _hubContext = hubContext;
            _encryptionClient = encryptionClient;
            _logger = logger;
            _dbContext = dbContext;
        }

        public async Task Consume(ConsumeContext<PublishMessageCommand> context)
        {
            try
            {
                _logger.LogInformation("Обробка команди PublishMessageCommand. MessageId: {MessageId}",
                    context.Message.MessageId);

                var existingEvent = await _dbContext.ProcessedEvents
                .FirstOrDefaultAsync(e => e.EventId == context.Message.CorrelationId &&
                                         e.EventType == "MessagePublished");

                if (existingEvent != null)
                {
                    // Повідомлення вже було опубліковано, повертаємо успішний результат
                    await context.Publish(new MessagePublishedEvent
                    {
                        CorrelationId = context.Message.CorrelationId,
                        MessageId = context.Message.MessageId
                    });

                    return;
                }

                _logger.LogInformation("Публікація повідомлення {MessageId} через SignalR",
                    context.Message.MessageId);

                // Розшифровуємо вміст повідомлення перед надсиланням клієнтам
                string content;
                try
                {
                    // Вміст повідомлення у context.Message.Content вже зашифрований
                    content = await _encryptionClient.DecryptAsync(context.Message.Content);
                }
                catch (ServiceUnavailableException)
                {
                    // Якщо сервіс шифрування недоступний, показуємо заглушку
                    _logger.LogWarning("Сервіс шифрування недоступний. Повідомлення буде надіслано із заглушкою.");
                    content = "Повідомлення недоступне для відображення";
                }

                // Створюємо DTO для надсилання через SignalR
                var messageDto = new MessageDto
                {
                    Id = context.Message.MessageId,
                    ChatRoomId = context.Message.ChatRoomId,
                    ChatRoomType = context.Message.ChatRoomType,
                    SenderUserId = context.Message.SenderUserId,
                    Content = content, // Розшифрований вміст
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false,
                    IsEdited = false
                };

                // Надсилаємо повідомлення всім користувачам, підключеним до групи (чату)
                string groupName = context.Message.ChatRoomId.ToString();
                await _hubContext.Clients.Group(groupName).SendAsync("ReceiveMessage", messageDto);

                var processedEvent = new ProcessedEvent
                {
                    EventId = context.Message.CorrelationId,
                    EventType = "MessagePublished",
                    ProcessedAt = DateTime.UtcNow
                };

                await _dbContext.ProcessedEvents.AddAsync(processedEvent);
                await _dbContext.SaveChangesAsync();

                // Публікуємо подію успішної публікації
                await context.Publish(new MessagePublishedEvent
                {
                    CorrelationId = context.Message.CorrelationId,
                    MessageId = context.Message.MessageId
                });

                _logger.LogInformation("Повідомлення {MessageId} успішно опубліковано через SignalR",
                    context.Message.MessageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при публікації повідомлення {MessageId} через SignalR",
                    context.Message.MessageId);

                // У випадку помилки публікуємо подію про невдачу
                await context.Publish(new MessageDeliveryFailedEvent
                {
                    CorrelationId = context.Message.CorrelationId,
                    MessageId = context.Message.MessageId,
                    Reason = $"Помилка публікації повідомлення: {ex.Message}"
                });
            }
        }
    }
}
