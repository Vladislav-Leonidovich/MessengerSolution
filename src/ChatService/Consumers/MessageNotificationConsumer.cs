using MassTransit;
using Shared.Contracts;

namespace ChatService.Consumers
{
    // Споживач події створення повідомлення
    public class MessageNotificationConsumer : IConsumer<MessageEvents>
    {
        public async Task Consume(ConsumeContext<MessageEvents> context)
        {
            var messageEvent = context.Message;
            // Реалізуйте логіку, наприклад, оновлення стану чату, розсилку сповіщень тощо.
            Console.WriteLine($"Новe повідомлення в чаті {messageEvent.ChatRoomId}: {messageEvent.Content}");
            // Можна додати збереження інформації в базу, відправку сповіщень користувачам і т.д.
            await Task.CompletedTask;
        }
    }
}
