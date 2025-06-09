using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.DTOs.Common;

namespace MessageService.Sagas.MessageDelivery.Events
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
        public MessageStatus Status { get; set; } = MessageStatus.Saved; // Статус повідомлення після збереження
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
        public MessageStatus Status { get; set; } = MessageStatus.Published; // Статус повідомлення після публікації
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
        public MessageStatus Status { get; set; } = MessageStatus.Delivered; // Статус повідомлення після публікації
    }

    // Подія помилки доставки
    public class MessageDeliveryFailedEvent
    {
        public Guid CorrelationId { get; set; }
        public int MessageId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public MessageStatus Status { get; set; } = MessageStatus.Failed; // Статус повідомлення після публікації
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

    public class MessageDeliveryCompletedWithTimeoutEvent
    {
        public Guid CorrelationId { get; set; }
        public int MessageId { get; set; }
        public int ChatRoomId { get; set; }
        public List<int> DeliveredToUserIds { get; set; } = new List<int>();
        public bool TimedOut { get; set; }
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    }

    // Команда компенсації збереження повідомлення
    public class CompensateMessageSavingCommand
    {
        public Guid CorrelationId { get; set; }
        public int MessageId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    // Подія успішної компенсації збереження
    public class MessageSavingCompensatedEvent
    {
        public Guid CorrelationId { get; set; }
        public int MessageId { get; set; }
    }

    // Команда компенсації публікації повідомлення
    public class CompensateMessagePublishingCommand
    {
        public Guid CorrelationId { get; set; }
        public int MessageId { get; set; }
        public int ChatRoomId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    // Подія успішної компенсації публікації
    public class MessagePublishingCompensatedEvent
    {
        public Guid CorrelationId { get; set; }
        public int MessageId { get; set; }
    }

    // Подія помилки компенсації
    public class CompensationFailedEvent
    {
        public Guid CorrelationId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    // Подія таймауту компенсації
    public class CompensationTimeoutExpiredEvent
    {
        public Guid CorrelationId { get; set; }
    }
}
