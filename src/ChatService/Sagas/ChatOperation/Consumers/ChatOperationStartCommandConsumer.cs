using ChatService.Data;
using ChatService.Sagas.ChatOperation.Events;
using ChatService.Services.Interfaces;
using MassTransit;
using Shared.Consumers;

namespace ChatService.Sagas.ChatOperation.Consumers
{
    public class ChatOperationStartCommandConsumer : IdempotentConsumer<ChatOperationStartCommand, ChatDbContext>
    {
        private readonly IChatOperationService _operationService;
        private readonly ILogger<ChatOperationStartCommandConsumer> _logger;

        public ChatOperationStartCommandConsumer(
            IChatOperationService operationService,
            ChatDbContext dbContext,
            ILogger<ChatOperationStartCommandConsumer> logger)
            : base(dbContext, logger)
        {
            _operationService = operationService;
            _logger = logger;
        }

        protected override async Task ProcessEventAsync(ConsumeContext<ChatOperationStartCommand> context)
        {
            var command = context.Message;

            _logger.LogInformation("Створення операції відстеження {OperationType} для чату {ChatRoomId}. CorrelationId: {CorrelationId}",
                command.OperationType, command.ChatRoomId, command.CorrelationId);

            try
            {
                await _operationService.StartOperationAsync(
                    command.CorrelationId,
                    command.OperationType,
                    command.ChatRoomId,
                    command.UserId,
                    command.OperationData);

                _logger.LogInformation("Операцію відстеження {CorrelationId} успішно створено", command.CorrelationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при створенні операції відстеження {CorrelationId}", command.CorrelationId);
                throw;
            }
        }
    }
}
