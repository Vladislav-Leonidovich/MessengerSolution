using MassTransit;

namespace ChatService.Sagas.ChatCreation
{
    public class ChatCreationSagaState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public string CurrentState { get; set; } = string.Empty;
        public int ChatRoomId { get; set; }
        public int CreatorUserId { get; set; }
        public string MemberIdsJson { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
