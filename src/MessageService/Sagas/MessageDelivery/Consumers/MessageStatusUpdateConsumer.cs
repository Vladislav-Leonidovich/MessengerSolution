using MassTransit;
using MessageService.Repositories.Interfaces;
using MessageService.Sagas.MessageDelivery.Events;

namespace MessageService.Sagas.MessageDelivery.Consumers
{
    public class MessageStatusUpdateConsumer :
    IConsumer<MessageSavedEvent>,
    IConsumer<MessagePublishedEvent>,
    IConsumer<MessageDeliveryCompletedEvent>,
    IConsumer<MessageDeliveryFailedEvent>
    {
        private readonly IMessageRepository _messageRepository;
        private readonly ILogger<MessageStatusUpdateConsumer> _logger;

        public MessageStatusUpdateConsumer(
            IMessageRepository messageRepository,
            ILogger<MessageStatusUpdateConsumer> logger)
        {
            _messageRepository = messageRepository;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<MessageSavedEvent> context)
        {
            var message = context.Message;
            _logger.LogInformation("Отримано подію MessageSavedEvent для повідомлення {MessageId}", message.MessageId);
            await _messageRepository.UpdateMessageStatusAsync(message.MessageId, message.Status);
        }

        public async Task Consume(ConsumeContext<MessagePublishedEvent> context)
        {
            var message = context.Message;
            _logger.LogInformation("Отримано подію MessagePublishedEvent для повідомлення {MessageId}", message.MessageId);
            await _messageRepository.UpdateMessageStatusAsync(message.MessageId, message.Status);
        }

        public async Task Consume(ConsumeContext<MessageDeliveryCompletedEvent> context)
        {
            var message = context.Message;
            _logger.LogInformation("Отримано подію MessageDeliveryCompletedEvent для повідомлення {MessageId}", message.MessageId);
            await _messageRepository.UpdateMessageStatusAsync(message.MessageId, message.Status);
        }

        public async Task Consume(ConsumeContext<MessageDeliveryFailedEvent> context)
        {
            var message = context.Message;
            _logger.LogInformation("Отримано подію MessageDeliveryFailedEvent для повідомлення {MessageId}", message.MessageId);
            await _messageRepository.UpdateMessageStatusAsync(message.MessageId, message.Status);
        }
    }
}
