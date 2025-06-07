using MassTransit;
using MessageService.Data;
using MessageService.Sagas.MessageOperation.Events;
using MessageService.Services.Interfaces;
using Shared.Consumers;

namespace MessageService.Sagas.MessageOperation.Consumers
{
    public class MessageOperationStartCommandConsumer : IdempotentConsumer<MessageOperationStartCommand, MessageDbContext>
    {
        private readonly IMessageOperationService _operationService;
        private readonly ILogger<MessageOperationStartCommandConsumer> _logger;

        public MessageOperationStartCommandConsumer(
            IMessageOperationService operationService,
            MessageDbContext dbContext,
            ILogger<MessageOperationStartCommandConsumer> logger)
            : base(dbContext, logger)
        {
            _operationService = operationService;
            _logger = logger;
        }
        protected override async Task ProcessEventAsync(ConsumeContext<MessageOperationStartCommand> context)
        {
            var command = context.Message;

            _logger.LogInformation("Створення операції відстеження {OperationType} для повідомлення {MessageId}. CorrelationId: {CorrelationId}",
                command.OperationType, command.MessageId, command.CorrelationId);

            try
            {
                await _operationService.StartOperationAsync(
                                    command.CorrelationId,
                                    command.OperationType,
                                    command.UserId,
                                    command.MessageId,
                                    command.ChatRoomId,
                                    command.OperationData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при створенні операції відстеження {CorrelationId}", command.CorrelationId);
                throw;
            }
        }
    }
}
