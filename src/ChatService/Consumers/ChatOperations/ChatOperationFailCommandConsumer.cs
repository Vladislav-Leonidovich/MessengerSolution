using ChatService.Data;
using ChatService.Services.Interfaces;
using MassTransit;
using Shared.Consumers;
using Shared.Sagas;

namespace ChatService.Consumers.ChatOperations
{
    public class ChatOperationFailCommandConsumer : IdempotentConsumer<ChatOperationFailCommand, ChatDbContext>
    {
        private readonly IChatOperationService _operationService;
        private readonly ILogger<ChatOperationFailCommandConsumer> _logger;

        public ChatOperationFailCommandConsumer(
            IChatOperationService operationService,
            ChatDbContext dbContext,
            ILogger<ChatOperationFailCommandConsumer> logger)
            : base(dbContext, logger)
        {
            _operationService = operationService;
            _logger = logger;
        }

        protected override async Task ProcessEventAsync(ConsumeContext<ChatOperationFailCommand> context)
        {
            var command = context.Message;

            try
            {
                await _operationService.FailOperationAsync(
                    command.CorrelationId,
                    command.ErrorMessage,
                    command.ErrorCode);

                _logger.LogInformation("Операцію {CorrelationId} позначено як невдалу: {ErrorMessage}",
                    command.CorrelationId, command.ErrorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при позначенні операції {CorrelationId} як невдалої", command.CorrelationId);
                throw;
            }
        }
    }
}
