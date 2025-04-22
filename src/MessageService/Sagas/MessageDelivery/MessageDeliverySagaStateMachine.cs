using Grpc.Core;
using MassTransit;
using MessageService.Services.Interfaces;
using MessageServiceDTOs;
using Shared.Sagas;

namespace MessageService.Sagas.MessageDelivery
{
    public class MessageDeliverySagaStateMachine : MassTransitStateMachine<MessageDeliverySagaState>
    {
        public MessageDeliverySagaStateMachine()
        {
            // Налаштування стану саги
            InstanceState(x => x.CurrentState);

            // Визначення подій та кореляція з інстансами саги
            Event(() => MessageDeliveryStarted, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => MessageSaved, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => MessagePublished, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => MessageDeliveredToUser, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => DeliveryStatusChecked, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => MessageDeliveryFailed, x => x.CorrelateById(context => context.Message.CorrelationId));

            // Початковий стан і перехід до збереження повідомлення
            Initially(
                When(MessageDeliveryStarted)
                    .Then(context =>
                    {
                        // Ініціалізація даних саги
                        context.Saga.MessageId = context.Message.MessageId;
                        context.Saga.ChatRoomId = context.Message.ChatRoomId;
                        context.Saga.ChatRoomType = context.Message.ChatRoomType;
                        context.Saga.SenderUserId = context.Message.SenderUserId;
                        context.Saga.CreatedAt = DateTime.UtcNow;
                    })
                    .Publish(context => new SaveMessageCommand
                    {
                        CorrelationId = context.Message.CorrelationId,
                        MessageId = context.Message.MessageId,
                        ChatRoomId = context.Message.ChatRoomId,
                        ChatRoomType = context.Message.ChatRoomType,
                        SenderUserId = context.Message.SenderUserId,
                        Content = context.Message.Content
                    })
                    .TransitionTo(SavingMessage)
            );

            // Стан збереження повідомлення
            During(SavingMessage,
                When(MessageSaved)
                    .Then(context =>
                    {
                        context.Saga.IsSaved = true;
                        context.Saga.EncryptedContent = context.Message.EncryptedContent;
                    })
                    .Publish(context => new PublishMessageCommand
                    {
                        CorrelationId = context.Message.CorrelationId,
                        MessageId = context.Message.MessageId,
                        ChatRoomId = context.Saga.ChatRoomId,
                        ChatRoomType = context.Saga.ChatRoomType,
                        SenderUserId = context.Saga.SenderUserId,
                        Content = context.Message.EncryptedContent
                    })
                    .TransitionTo(PublishingMessage),

                When(MessageDeliveryFailed)
                    .Then(context => context.Saga.ErrorMessage = context.Message.Reason)
                    .TransitionTo(Failed)
            );

            // Стан публікації повідомлення
            During(PublishingMessage,
                When(MessagePublished)
                    .Then(context => context.Saga.IsPublished = true)
                    .TransitionTo(WaitingDeliveryConfirmation),

                When(MessageDeliveryFailed)
                    .Then(context => context.Saga.ErrorMessage = context.Message.Reason)
                    .TransitionTo(Failed)
            );

            // Стан очікування підтвердження доставки
            During(WaitingDeliveryConfirmation,
                When(MessageDeliveredToUser)
                    .Then(context =>
                    {
                        // Додати користувача до списку тих, кому доставлено
                        if (!context.Saga.DeliveredToUserIds.Contains(context.Message.UserId))
                        {
                            context.Saga.DeliveredToUserIds.Add(context.Message.UserId);
                        }
                    })
                    .Publish(context => new CheckDeliveryStatusCommand
                    {
                        CorrelationId = context.Message.CorrelationId,
                        MessageId = context.Saga.MessageId,
                        ChatRoomId = context.Saga.ChatRoomId,
                        ChatRoomType = context.Saga.ChatRoomType
                    }),

                When(DeliveryStatusChecked)
                    .If(context => context.Message.IsDeliveredToAll,
                        binder => binder.TransitionTo(Completed))
                    .Else(binder => binder.Stay()),

                When(MessageDeliveryFailed)
                    .Then(context => context.Saga.ErrorMessage = context.Message.Reason)
                    .TransitionTo(Failed)
            );

            // Налаштування тайм-ауту для доставки
            Schedule(() => DeliveryTimeoutExpired,
                saga => saga.DeliveryTimeoutTokenId,
                s => s
                    .StartAt(context => DateTime.UtcNow.AddMinutes(5))
                    .OnComplete(context => new MessageDeliveryTimeoutEvent
                    {
                        CorrelationId = context.Saga.CorrelationId,
                        MessageId = context.Saga.MessageId
                    })
            );

            // Обробка тайм-ауту доставки
            During(WaitingDeliveryConfirmation,
                When(DeliveryTimeoutExpired.Received)
                    .Then(context =>
                    {
                        // Помічаємо, що час вийшов, але сага завершується успішно
                        context.Saga.IsDeliveredAfterTimeout = true;
                    })
                    .TransitionTo(Completed)
            );

            // Автоматичне завершення саги
            SetCompletedWhenFinalized();
        }

        // Стани
        public State? SavingMessage { get; private set; }
        public State? PublishingMessage { get; private set; }
        public State? WaitingDeliveryConfirmation { get; private set; }
        public State? Completed { get; private set; }
        public State? Failed { get; private set; }

        // Події
        public Event<MessageDeliveryStartedEvent>? MessageDeliveryStarted { get; private set; }
        public Event<MessageSavedEvent>? MessageSaved { get; private set; }
        public Event<MessagePublishedEvent>? MessagePublished { get; private set; }
        public Event<MessageDeliveredToUserEvent>? MessageDeliveredToUser { get; private set; }
        public Event<DeliveryStatusCheckedEvent>? DeliveryStatusChecked { get; private set; }
        public Event<MessageDeliveryFailedEvent>? MessageDeliveryFailed { get; private set; }

        // Таймаут доставки
        public Schedule<MessageDeliverySagaState, MessageDeliveryTimeoutEvent>? DeliveryTimeoutExpired { get; private set; }
    }
}
