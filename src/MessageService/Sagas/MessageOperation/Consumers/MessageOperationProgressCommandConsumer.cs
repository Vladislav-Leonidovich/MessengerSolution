using MassTransit;
using MessageService.Data;
using MessageService.Sagas.MessageOperation.Events;
using MessageService.Services.Interfaces;
using Shared.Consumers;

namespace MessageService.Sagas.MessageOperation.Consumers
{
    public class MessageOperationProgressCommandConsumer : IdempotentConsumer<MessageOperationProgressCommand, MessageDbContext>
    {
        private readonly IMessageOperationService _operationService;
        private readonly ILogger<MessageOperationProgressCommandConsumer> _logger;
        public MessageOperationProgressCommandConsumer(
            IMessageOperationService operationService,
            MessageDbContext dbContext,
            ILogger<MessageOperationProgressCommandConsumer> logger)
            : base(dbContext, logger)
        {
            _operationService = operationService;
            _logger = logger;
        }
        protected override async Task ProcessEventAsync(ConsumeContext<MessageOperationProgressCommand> context)
        {
            var command = context.Message;

            _logger.LogInformation("Оновлення прогресу операції {CorrelationId}",
                command.CorrelationId);

            try
            {
                await _operationService.UpdateProgressAsync(
                    command.CorrelationId,
                    command.Progress,
                    command.StatusMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при оновленні прогресу операції {CorrelationId}", command.CorrelationId);
                throw;
            }
        }
    }
}
