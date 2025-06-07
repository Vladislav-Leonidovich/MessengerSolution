using ChatService.Data;
using ChatService.Sagas.ChatCreation.Events;
using ChatService.Services.Interfaces;
using MassTransit;
using Shared.Consumers;
using ChatService.Sagas.ChatOperation.Events;

namespace ChatService.Sagas.ChatOperation.Consumers
{
    public class ChatOperationProgressCommandConsumer : IdempotentConsumer<ChatOperationProgressCommand, ChatDbContext>
    {
        private readonly IChatOperationService _operationService;
        private readonly ILogger<ChatOperationProgressCommandConsumer> _logger;

        public ChatOperationProgressCommandConsumer(
            IChatOperationService operationService,
            ChatDbContext dbContext,
            ILogger<ChatOperationProgressCommandConsumer> logger)
            : base(dbContext, logger)
        {
            _operationService = operationService;
            _logger = logger;
        }

        protected override async Task ProcessEventAsync(ConsumeContext<ChatOperationProgressCommand> context)
        {
            var command = context.Message;

            try
            {
                await _operationService.UpdateProgressAsync(
                    command.CorrelationId,
                    command.Progress,
                    command.StatusMessage);

                _logger.LogInformation("Оновлено прогрес операції {CorrelationId}: {Progress}%",
                    command.CorrelationId, command.Progress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при оновленні прогресу операції {CorrelationId}", command.CorrelationId);
                throw;
            }
        }
    }
}
