using ChatService.Data;
using ChatService.Services.Interfaces;
using MassTransit;
using Shared.Consumers;
using ChatService.Sagas.ChatOperation.Events;

namespace ChatService.Sagas.ChatOperation.Consumers
{
    public class ChatOperationCompleteCommandConsumer : IdempotentConsumer<ChatOperationCompleteCommand, ChatDbContext>
    {
        private readonly IChatOperationService _operationService;
        private readonly ILogger<ChatOperationCompleteCommandConsumer> _logger;

        public ChatOperationCompleteCommandConsumer(
            IChatOperationService operationService,
            ChatDbContext dbContext,
            ILogger<ChatOperationCompleteCommandConsumer> logger)
            : base(dbContext, logger)
        {
            _operationService = operationService;
            _logger = logger;
        }

        protected override async Task ProcessEventAsync(ConsumeContext<ChatOperationCompleteCommand> context)
        {
            var command = context.Message;

            try
            {
                await _operationService.CompleteOperationAsync(
                    command.CorrelationId,
                    command.Result);

                _logger.LogInformation("Операцію {CorrelationId} успішно завершено", command.CorrelationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при завершенні операції {CorrelationId}", command.CorrelationId);
                throw;
            }
        }
    }
}
