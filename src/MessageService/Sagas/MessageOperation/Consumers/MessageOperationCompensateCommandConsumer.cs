using MassTransit;
using MessageService.Data;
using MessageService.Sagas.MessageOperation.Events;
using MessageService.Services.Interfaces;
using Shared.Consumers;

namespace MessageService.Sagas.MessageOperation.Consumers
{
    public class MessageOperationCompensateCommandConsumer : IdempotentConsumer<MessageOperationCompensateCommand, MessageDbContext>
    {
        private readonly IMessageOperationService _operationService;
        private readonly ILogger<MessageOperationCompensateCommandConsumer> _logger;
        public MessageOperationCompensateCommandConsumer(
            IMessageOperationService operationService,
            MessageDbContext dbContext,
            ILogger<MessageOperationCompensateCommandConsumer> logger)
            : base(dbContext, logger)
        {
            _operationService = operationService;
            _logger = logger;
        }
        protected override async Task ProcessEventAsync(ConsumeContext<MessageOperationCompensateCommand> context)
        {
            var command = context.Message;
            _logger.LogInformation("Компенсація операції {CorrelationId}", command.CorrelationId);
            try
            {
                await _operationService.CompensateOperationAsync(
                    command.CorrelationId,
                    command.Reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при компенсації операції {CorrelationId}", command.CorrelationId);
                throw;
            }
        }
    }
    {
    }
}
