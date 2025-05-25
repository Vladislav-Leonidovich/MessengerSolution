using ChatService.Data;
using ChatService.Services.Interfaces;
using MassTransit;
using Shared.Consumers;
using Shared.Sagas;

namespace ChatService.Consumers.ChatOperations
{
    public class ChatOperationCompensateCommandConsumer : IdempotentConsumer<ChatOperationCompensateCommand, ChatDbContext>
    {
        private readonly IChatOperationService _operationService;
        private readonly ILogger<ChatOperationCompensateCommandConsumer> _logger;

        public ChatOperationCompensateCommandConsumer(
            IChatOperationService operationService,
            ChatDbContext dbContext,
            ILogger<ChatOperationCompensateCommandConsumer> logger)
            : base(dbContext, logger)
        {
            _operationService = operationService;
            _logger = logger;
        }

        protected override async Task ProcessEventAsync(ConsumeContext<ChatOperationCompensateCommand> context)
        {
            var command = context.Message;

            try
            {
                await _operationService.CompensateOperationAsync(
                    command.CorrelationId,
                    command.Reason);

                _logger.LogInformation("Операцію {CorrelationId} компенсовано: {Reason}",
                    command.CorrelationId, command.Reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при компенсації операції {CorrelationId}", command.CorrelationId);
                throw;
            }
        }
    }
}
