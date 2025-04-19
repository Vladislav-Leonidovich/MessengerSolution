using MassTransit;
using Shared.Sagas;

namespace ChatService.Sagas.ChatCreation
{
    public class ChatCreationSagaStateMachine : MassTransitStateMachine<ChatCreationSagaState>
    {
        public ChatCreationSagaStateMachine()
        {
            InstanceState(x => x.CurrentState);

            // Начальное событие для запуска саги
            Event(() => ChatCreationStarted, x => x.CorrelateById(context => context.Message.CorrelationId));

            // События для шагов саги
            Event(() => ChatRoomCreated, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => MessageServiceNotified, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => FailureOccurred, x => x.CorrelateById(context => context.Message.CorrelationId));

            // Определение состояний
            Initially(
                When(ChatCreationStarted)
                    .Then(context =>
                    {
                        context.Saga.ChatRoomId = context.Message.ChatRoomId;
                        context.Saga.CreatorUserId = context.Message.CreatorUserId;
                        context.Saga.MemberIdsJson = JsonSerializer.Serialize(context.Message.MemberIds);
                        context.Saga.CreatedAt = DateTime.UtcNow;
                    })
                    .Publish(context => new CreateChatRoomCommand
                    {
                        CorrelationId = context.Message.CorrelationId,
                        ChatRoomId = context.Message.ChatRoomId,
                        CreatorUserId = context.Message.CreatorUserId,
                        MemberIds = context.Message.MemberIds
                    })
                    .TransitionTo(CreatingChatRoom)
            );

            During(CreatingChatRoom,
                When(ChatRoomCreated)
                    .Publish(context => new NotifyMessageServiceCommand
                    {
                        CorrelationId = context.Message.CorrelationId,
                        ChatRoomId = context.Message.ChatRoomId
                    })
                    .TransitionTo(NotifyingMessageService),

                When(FailureOccurred)
                    .Then(context => context.Saga.ErrorMessage = context.Message.Reason)
                    .Publish(context => new CompensateChatCreationCommand
                    {
                        CorrelationId = context.Saga.CorrelationId,
                        ChatRoomId = context.Saga.ChatRoomId
                    })
                    .TransitionTo(Compensating)
            );

            During(NotifyingMessageService,
                When(MessageServiceNotified)
                    .Publish(context => new CompleteChatCreationCommand
                    {
                        CorrelationId = context.Saga.CorrelationId,
                        ChatRoomId = context.Saga.ChatRoomId
                    })
                    .TransitionTo(Completed),

                When(FailureOccurred)
                    .Then(context => context.Saga.ErrorMessage = context.Message.Reason)
                    .Publish(context => new CompensateChatCreationCommand
                    {
                        CorrelationId = context.Saga.CorrelationId,
                        ChatRoomId = context.Saga.ChatRoomId
                    })
                    .TransitionTo(Compensating)
            );

            During(Compensating,
                When(ChatCreationCompensated)
                    .TransitionTo(Failed)
            );

            // Настраиваем автоматическое завершение саги
            SetCompletedWhenFinalized();
        }

        // Состояния
        public State CreatingChatRoom { get; private set; }
        public State NotifyingMessageService { get; private set; }
        public State Completed { get; private set; }
        public State Compensating { get; private set; }
        public State Failed { get; private set; }

        // События
        public Event<ChatCreationStartedEvent> ChatCreationStarted { get; private set; }
        public Event<ChatRoomCreatedEvent> ChatRoomCreated { get; private set; }
        public Event<MessageServiceNotifiedEvent> MessageServiceNotified { get; private set; }
        public Event<ChatCreationFailedEvent> FailureOccurred { get; private set; }
        public Event<ChatCreationCompensatedEvent> ChatCreationCompensated { get; private set; }
    }
}
