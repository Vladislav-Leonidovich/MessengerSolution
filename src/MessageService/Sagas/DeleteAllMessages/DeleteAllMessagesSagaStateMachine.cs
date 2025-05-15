using MassTransit;
using MessageService.Sagas.MessageDelivery;
using Shared.Contracts;
using Shared.Sagas;

namespace MessageService.Sagas.DeleteAllMessages
{
    public class DeleteAllMessagesSagaStateMachine : MassTransitStateMachine<DeleteAllMessagesSagaState>
    {
        public DeleteAllMessagesSagaStateMachine()
        {
            InstanceState(x => x.CurrentState);

            // Налаштування таймауту
            Schedule(() => OperationTimeout,
                saga => saga.TimeoutTokenId,
                s => {
                    s.Received = r => r.CorrelateById(context => context.Message.CorrelationId);
                    s.Delay = TimeSpan.FromMinutes(5); // Таймаут 5 хвилин
                });

            // Визначення подій
            Event(() => DeleteAllMessagesRequested, x => x.CorrelateById(context => context.Message.CorrelationId));
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
                        context.Saga.LastUpdatedAt = DateTime.UtcNow;
                    })
                    .Schedule(OperationTimeout,
                        context => new DeleteMessagesSagaTimeoutEvent
                        {
                            CorrelationId = context.Message.CorrelationId,
                            TimeoutReason = "Timeout waiting for messages deletion"
                        })
                    .Publish(context => new DeleteChatMessagesCommand
                    {
                        CorrelationId = context.Message.CorrelationId,
                        ChatRoomId = context.Message.ChatRoomId,
                        InitiatedByUserId = context.Message.InitiatedByUserId
                    })
                    .TransitionTo(DeletingMessages)
            );

            // Стан видалення повідомлень
            During(DeletingMessages,
                When(MessagesDeleted)
                    .Then(context =>
                    {
                        context.Saga.DeletedMessageCount = context.Message.MessageCount;
                        context.Saga.LastUpdatedAt = DateTime.UtcNow;
                    })
                    .Unschedule(OperationTimeout) // Скасовуємо таймаут
                    .Schedule(OperationTimeout,
                        context => new DeleteMessagesSagaTimeoutEvent
                        {
                            CorrelationId = context.Message.CorrelationId,
                            TimeoutReason = "Timeout waiting for notifications"
                        })
                    .Publish(context => new SendChatNotificationCommand
                    {
                        CorrelationId = context.Message.CorrelationId,
                        ChatRoomId = context.Saga.ChatRoomId,
                        Message = $"Всі {context.Message.MessageCount} повідомлень видалено користувачем {context.Saga.InitiatedByUserId}"
                    })
                    .TransitionTo(SendingNotifications),

                When(ErrorOccurred)
                    .Then(context =>
                    {
                        context.Saga.ErrorMessage = context.Message.ErrorMessage;
                        context.Saga.LastError = context.Message.ErrorMessage;
                        context.Saga.LastUpdatedAt = DateTime.UtcNow;
                    })
                    .Unschedule(OperationTimeout)
                    .TransitionTo(Failed),

                When(OperationTimeout.Received)
                    .Then(context =>
                    {
                        context.Saga.ErrorMessage = "Таймаут операції видалення повідомлень";
                        context.Saga.LastError = context.Message.TimeoutReason;
                        context.Saga.LastUpdatedAt = DateTime.UtcNow;
                    })
                    .TransitionTo(Failed)
            );

            // Стан надсилання сповіщень
            During(SendingNotifications,
                When(NotificationsSent)
                    .Then(context =>
                    {
                        context.Saga.IsCompleted = true;
                        context.Saga.CompletedAt = DateTime.UtcNow;
                        context.Saga.LastUpdatedAt = DateTime.UtcNow;
                    })
                    .Unschedule(OperationTimeout)
                    .TransitionTo(Completed),

                When(ErrorOccurred)
                    .Then(context =>
                    {
                        context.Saga.ErrorMessage = context.Message.ErrorMessage;
                        context.Saga.LastError = context.Message.ErrorMessage;
                        context.Saga.LastUpdatedAt = DateTime.UtcNow;
                        context.Saga.RetryCount++;
                    })
                    .IfElse(
                        context => context.Saga.RetryCount < 3,
                        retry => retry
                            .Publish(context => new SendChatNotificationCommand
                            {
                                CorrelationId = context.Message.CorrelationId,
                                ChatRoomId = context.Saga.ChatRoomId,
                                Message = $"Спроба {context.Saga.RetryCount + 1}: Всі повідомлення видалено користувачем {context.Saga.InitiatedByUserId}"
                            }),
                        fail => fail
                            .Unschedule(OperationTimeout)
                            .TransitionTo(Failed)
                    ),

                When(OperationTimeout.Received)
                    .Then(context =>
                    {
                        context.Saga.ErrorMessage = "Таймаут операції надсилання сповіщень";
                        context.Saga.LastError = context.Message.TimeoutReason;
                        context.Saga.LastUpdatedAt = DateTime.UtcNow;
                    })
                    .TransitionTo(Failed)
            );

            // Стан помилки
            During(Failed,
                Ignore(DeleteAllMessagesRequested),
                Ignore(MessagesDeleted),
                Ignore(NotificationsSent),
                Ignore(ErrorOccurred)
            );

            // Стан завершення
            During(Completed,
                Ignore(DeleteAllMessagesRequested),
                Ignore(MessagesDeleted),
                Ignore(NotificationsSent),
                Ignore(ErrorOccurred)
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

        // Розклад таймауту
        public Schedule<DeleteAllMessagesSagaState, DeleteMessagesSagaTimeoutEvent> OperationTimeout { get; private set; }
    }
}
