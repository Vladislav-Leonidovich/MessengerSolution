using MassTransit;
using MessageService.Data;
using MessageService.Sagas.MessageOperation.Events;
using MessageService.Services.Interfaces;
using Shared.Consumers;

namespace MessageService.Sagas.MessageOperation.Consumers
{
    public class MessageOperationCompleteCommandConsumer : IdempotentConsumer<MessageOperationCompleteCommand, MessageDbContext>
    {
        private readonly IMessageOperationService _operationService;
        private readonly ILogger<MessageOperationCompleteCommandConsumer> _logger;
        public MessageOperationCompleteCommandConsumer(
            IMessageOperationService operationService,
            MessageDbContext dbContext,
            ILogger<MessageOperationCompleteCommandConsumer> logger)
            : base(dbContext, logger)
        {
            _operationService = operationService;
            _logger = logger;
        }
        protected override async Task ProcessEventAsync(ConsumeContext<MessageOperationCompleteCommand> context)
        {
            var command = context.Message;
            _logger.LogInformation("Завершення операції {CorrelationId}", command.CorrelationId);
            try
            {
                await _operationService.CompleteOperationAsync(command.CorrelationId, command.Result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при завершенні операції {CorrelationId}", command.CorrelationId);
                throw;
            }
        }
    }
    {
    }
}
