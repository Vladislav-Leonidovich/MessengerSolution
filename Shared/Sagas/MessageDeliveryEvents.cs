using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessageServiceDTOs;

namespace Shared.Sagas
{
    // Подія початку процесу доставки повідомлення
    public class MessageDeliveryStartedEvent
    {
        public Guid CorrelationId { get; set; } = Guid.NewGuid();
        public int MessageId { get; set; }
        public int ChatRoomId { get; set; }
        public int SenderUserId { get; set; }
        public string Content { get; set; } = string.Empty;
    }

    // Команда для збереження повідомлення
    public class SaveMessageCommand
    {
        public Guid CorrelationId { get; set; }
        public int MessageId { get; set; }
        public int ChatRoomId { get; set; }
        public int SenderUserId { get; set; }
        public string Content { get; set; } = string.Empty;
    }

    // Подія успішного збереження повідомлення
    public class MessageSavedEvent
    {
        public Guid CorrelationId { get; set; }
        public int MessageId { get; set; }
        public string EncryptedContent { get; set; } = string.Empty;
    }

    // Команда для публікації повідомлення в хаб SignalR
    public class PublishMessageCommand
    {
        public Guid CorrelationId { get; set; }
        public int MessageId { get; set; }
        public int ChatRoomId { get; set; }
        public int SenderUserId { get; set; }
        public string Content { get; set; } = string.Empty;
    }

    // Подія успішної публікації повідомлення
    public class MessagePublishedEvent
    {
        public Guid CorrelationId { get; set; }
        public int MessageId { get; set; }
    }

    // Подія доставки повідомлення конкретному користувачу
    public class MessageDeliveredToUserEvent
    {
        public Guid CorrelationId { get; set; }
        public int MessageId { get; set; }
        public int UserId { get; set; }
    }

    // Подія завершення процесу доставки
    public class MessageDeliveryCompletedEvent
    {
        public Guid CorrelationId { get; set; }
        public int MessageId { get; set; }
    }

    // Подія помилки доставки
    public class MessageDeliveryFailedEvent
    {
        public Guid CorrelationId { get; set; }
        public int MessageId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    // Команда для перевірки статусу доставки
    public class CheckDeliveryStatusCommand
    {
        public Guid CorrelationId { get; set; }
        public int MessageId { get; set; }
        public int ChatRoomId { get; set; }
        public int SenderUserId { get; set; }
    }

    // Подія результату перевірки статусу доставки
    public class DeliveryStatusCheckedEvent
    {
        public Guid CorrelationId { get; set; }
        public int MessageId { get; set; }
        public bool IsDeliveredToAll { get; set; }
    }

    public class MessageDeliveryTimeoutEvent
    {
        public Guid CorrelationId { get; set; }
        public int MessageId { get; set; }
    }
}
