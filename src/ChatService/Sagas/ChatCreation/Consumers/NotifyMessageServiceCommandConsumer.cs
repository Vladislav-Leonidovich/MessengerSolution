using ChatService.Data;
using MassTransit;
using Shared.Consumers;
using Shared.Sagas;

namespace ChatService.Sagas.ChatCreation.Consumers
{
    public class NotifyMessageServiceCommandConsumer : IdempotentConsumer<NotifyMessageServiceCommand, ChatDbContext>
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<NotifyMessageServiceCommandConsumer> _logger;

        public NotifyMessageServiceCommandConsumer(
            IHttpClientFactory httpClientFactory,
            ChatDbContext dbContext,
            ILogger<NotifyMessageServiceCommandConsumer> logger)
            : base(dbContext, logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        protected override async Task ProcessEventAsync(ConsumeContext<NotifyMessageServiceCommand> context)
        {
            var command = context.Message;

            _logger.LogInformation("Обробка команди NotifyMessageServiceCommand. " +
                "ChatRoomId: {ChatRoomId}, CorrelationId: {CorrelationId}",
                command.ChatRoomId, command.CorrelationId);

            try
            {
                // Створюємо HTTP клієнт для MessageService
                var client = _httpClientFactory.CreateClient("MessageService");

                // Підготовка даних для відправки
                var notificationData = new
                {
                    CorrelationId = command.CorrelationId,
                    ChatRoomId = command.ChatRoomId,
                    Timestamp = DateTime.UtcNow
                };

                // Відправляємо повідомлення про створення чату
                var response = await client.PostAsJsonAsync(
                    "api/internal/chat-created",
                    notificationData);

                // Перевіряємо успішність
                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Помилка при повідомленні MessageService: {response.StatusCode}. {errorContent}");
                }

                _logger.LogInformation("MessageService успішно повідомлено про створення чату {ChatRoomId}",
                    command.ChatRoomId);

                // Публікуємо подію успішного повідомлення MessageService
                await context.Publish(new MessageServiceNotifiedEvent
                {
                    CorrelationId = command.CorrelationId,
                    ChatRoomId = command.ChatRoomId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при повідомленні MessageService про створення чату {ChatRoomId}",
                    command.ChatRoomId);

                // Публікуємо подію про помилку
                await context.Publish(new ChatCreationFailedEvent
                {
                    CorrelationId = command.CorrelationId,
                    ChatRoomId = command.ChatRoomId,
                    Reason = $"Помилка при повідомленні MessageService: {ex.Message}"
                });

                throw; // Перекидаємо виняток для обробки транзакції в базовому класі
            }
        }
    }
}
