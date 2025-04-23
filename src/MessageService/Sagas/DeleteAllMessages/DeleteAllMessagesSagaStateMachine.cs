using MassTransit;
using Shared.Contracts;
using Shared.Sagas;

namespace MessageService.Sagas.DeleteAllMessages
{
    public class DeleteAllMessagesSagaStateMachine : MassTransitStateMachine<DeleteAllMessagesSagaState>
    {
        public DeleteAllMessagesSagaStateMachine()
        {
            InstanceState(x => x.CurrentState);

            // Початкова подія для запуску саги
            Event(() => DeleteAllMessagesRequested, x => x.CorrelateById(context => context.Message.CorrelationId));

            // Події для кроків саги
            Event(() => MessagesDeleted, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => NotificationsSent, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => ErrorOccurred, x => x.CorrelateById(context => context.Message.CorrelationId));

            // Початковий стан
            Initially(
                When(DeleteAllMessagesRequested)
                    .Then(context =>
                    {
                        context.Saga.ChatRoomId = context.Message.ChatRoomId;
                        context.Saga.InitiatedByUserId = context.Message.InitiatedByUserId;
                        context.Saga.StartedAt = DateTime.UtcNow;
                    })
                    .Publish(context => new DeleteChatMessagesCommand
                    {
                        CorrelationId = context.Message.CorrelationId,
                        ChatRoomId = context.Message.ChatRoomId
                    })
                    .TransitionTo(DeletingMessages)
            );

            // Стан видалення повідомлень
            During(DeletingMessages,
                When(MessagesDeleted)
                    .Publish(context => new SendChatNotificationCommand
                    {
                        CorrelationId = context.Message.CorrelationId,
                        ChatRoomId = context.Saga.ChatRoomId,
                        Message = $"Всі повідомлення видалено користувачем {context.Saga.InitiatedByUserId}"
                    })
                    .TransitionTo(SendingNotifications),

                When(ErrorOccurred)
                    .Then(context => context.Saga.ErrorMessage = context.Message.ErrorMessage)
                    .TransitionTo(Failed)
            );

            // Стан надсилання сповіщень учасникам чату
            During(SendingNotifications,
                When(NotificationsSent)
                    .Then(context => context.Saga.IsCompleted = true)
                    .TransitionTo(Completed),

                When(ErrorOccurred)
                    .Then(context => context.Saga.ErrorMessage = context.Message.ErrorMessage)
                    .TransitionTo(Failed)
            );

            // Автоматичне завершення саги
            SetCompletedWhenFinalized();
        }

        // Стани
        public State DeletingMessages { get; private set; }
        public State SendingNotifications { get; private set; }
        public State Completed { get; private set; }
        public State Failed { get; private set; }

        // Події
        public Event<DeleteAllChatMessagesCommand> DeleteAllMessagesRequested { get; private set; }
        public Event<MessagesDeletedEvent> MessagesDeleted { get; private set; }
        public Event<NotificationsSentEvent> NotificationsSent { get; private set; }
        public Event<ErrorEvent> ErrorOccurred { get; private set; }
    }
}
