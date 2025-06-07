using MassTransit;
using MessageService.Data;
using MessageService.Sagas.MessageOperation.Events;
using MessageService.Services.Interfaces;
using Shared.Consumers;

namespace MessageService.Sagas.MessageOperation.Consumers
{
    public class MessageOperationFailCommandConsumer : IdempotentConsumer<MessageOperationFailCommand, MessageDbContext>
    {
        private readonly IMessageOperationService _operationService;
        private readonly ILogger<MessageOperationFailCommandConsumer> _logger;
        public MessageOperationFailCommandConsumer(
            IMessageOperationService operationService,
            MessageDbContext dbContext,
            ILogger<MessageOperationFailCommandConsumer> logger)
            : base(dbContext, logger)
        {
            _operationService = operationService;
            _logger = logger;
        }
        protected override async Task ProcessEventAsync(ConsumeContext<MessageOperationFailCommand> context)
        {
            var command = context.Message;

            _logger.LogInformation("Обробка помилки операції {CorrelationId}", command.CorrelationId);
            try
            {
                await _operationService.FailOperationAsync(command.CorrelationId, command.ErrorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при обробці помилки операції {CorrelationId}", command.CorrelationId);
                throw;
            }
        }
    }
    {
    }
}
