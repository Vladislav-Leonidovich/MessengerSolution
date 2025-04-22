using MassTransit;
using MessageServiceDTOs;

namespace MessageService.Sagas.MessageDelivery
{
    public class MessageDeliverySagaState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public string CurrentState { get; set; } = string.Empty;

        // Дані про повідомлення
        public int MessageId { get; set; }
        public int ChatRoomId { get; set; }
        public ChatRoomType ChatRoomType { get; set; }
        public int SenderUserId { get; set; }
        public string EncryptedContent { get; set; } = string.Empty;

        // Дані про доставку
        public DateTime CreatedAt { get; set; }
        public bool IsSaved { get; set; }
        public bool IsEncrypted { get; set; }
        public bool IsPublished { get; set; }
        public bool IsDelivered { get; set; }
        public bool IsDeliveredAfterTimeout { get; set; }
        public List<int> DeliveredToUserIds { get; set; } = new List<int>();

        // Інформація про помилки
        public string ErrorMessage { get; set; } = string.Empty;

        // Для таймауту доставки
        public Guid? DeliveryTimeoutTokenId { get; set; }
    }
}
