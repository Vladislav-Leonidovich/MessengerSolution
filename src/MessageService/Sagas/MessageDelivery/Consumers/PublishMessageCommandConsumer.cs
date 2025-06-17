using MassTransit;
using MessageService.Data;
using MessageService.Hubs;
using MessageService.Services.Interfaces;
using Shared.DTOs.Message;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Shared.Consumers;
using Shared.Contracts;
using Shared.Exceptions;
using MessageService.Sagas.MessageDelivery.Events;

namespace MessageService.Sagas.MessageDelivery.Consumers
{
    public class PublishMessageCommandConsumer : IdempotentConsumer<PublishMessageCommand, MessageDbContext>
    {
        private readonly IHubContext<MessageHub> _hubContext;
        private readonly IEncryptionGrpcClient _encryptionClient;
        private readonly ILogger<PublishMessageCommandConsumer> _logger;

        public PublishMessageCommandConsumer(
            IHubContext<MessageHub> hubContext,
            IEncryptionGrpcClient encryptionClient,
            MessageDbContext dbContext,
            ILogger<PublishMessageCommandConsumer> logger)
            : base(dbContext, logger)
        {
            _hubContext = hubContext;
            _encryptionClient = encryptionClient;
            _logger = logger;
        }

        protected override async Task ProcessEventAsync(ConsumeContext<PublishMessageCommand> context)
        {
            var command = context.Message;

            _logger.LogInformation("Обробка команди PublishMessageCommand. MessageId: {MessageId}",
                command.MessageId);

            try
            {
                // Розшифровуємо вміст повідомлення перед надсиланням клієнтам
                string content;
                try
                {
                    // Вміст повідомлення у command.Content вже зашифрований
                    content = await _encryptionClient.EncryptAsync(command.Content);
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
                    Id = command.MessageId,
                    ChatRoomId = command.ChatRoomId,
                    SenderUserId = command.SenderUserId,
                    Content = command.Content, // Розшифрований вміст
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false,
                    IsEdited = false
                };

                // Надсилаємо повідомлення всім користувачам, підключеним до групи (чату)
                string groupName = command.ChatRoomId.ToString();
                await _hubContext.Clients.Group(groupName).SendAsync("ReceiveMessage", messageDto);

                // Публікуємо подію успішної публікації
                await context.Publish(new MessagePublishedEvent
                {
                    CorrelationId = command.CorrelationId,
                    MessageId = command.MessageId
                });

                _logger.LogInformation("Повідомлення {MessageId} успішно опубліковано через SignalR",
                    command.MessageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при публікації повідомлення {MessageId} через SignalR",
                    command.MessageId);

                // У випадку помилки публікуємо подію про невдачу
                await context.Publish(new MessageDeliveryFailedEvent
                {
                    CorrelationId = command.CorrelationId,
                    MessageId = command.MessageId,
                    Reason = $"Помилка публікації повідомлення: {ex.Message}"
                });

                throw; // Перекидаємо виняток для базового класу
            }
        }
    }
}
