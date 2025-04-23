using MassTransit;

namespace MessageService.Sagas.DeleteAllMessages
{
    public class DeleteAllMessagesSagaState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public string CurrentState { get; set; } = string.Empty;
        public int ChatRoomId { get; set; }
        public int InitiatedByUserId { get; set; }
        public DateTime StartedAt { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
    }
}
