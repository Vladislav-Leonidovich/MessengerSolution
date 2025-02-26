using EncryptionService.Helpers;
using MassTransit;
using Shared.Contracts;

namespace EncryptionService.Consumers
{
    public class MessageEncryptionConsumer : IConsumer<MessageCreatedEvent>
    {
        public async Task Consume(ConsumeContext<MessageCreatedEvent> context)
        {
            var messageEvent = context.Message;

            // Шифруємо текст повідомлення використовуючи заданий ключ.
            string encryptionKey = "YourDefaultEncryptionKey123";
            string encryptedContent = EncryptionHelper.EncryptString(messageEvent.Content, encryptionKey);

            // Логування для перевірки
            Console.WriteLine($"Message {messageEvent.MessageId} from chat {messageEvent.ChatRoomId} зашифровано: {encryptedContent}");

            // За потреби:
            // - Оновити дані в базі даних (наприклад, зберегти зашифрований вміст)
            await Task.CompletedTask;
        }
    }
}
