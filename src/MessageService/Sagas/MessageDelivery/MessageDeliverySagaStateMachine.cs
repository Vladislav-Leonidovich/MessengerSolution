using Grpc.Core;
using MassTransit;
using MessageService.Services.Interfaces;
using MessageService.Sagas.MessageDelivery.Events;
using Shared.DTOs.Chat;
using MessageService.Sagas.MessageOperation.Events;
using Shared.DTOs.Message;

namespace MessageService.Sagas.MessageDelivery
{
    public class MessageDeliverySagaStateMachine : MassTransitStateMachine<MessageDeliverySagaState>
    {
        public MessageDeliverySagaStateMachine(ILogger<MessageDeliverySagaStateMachine> logger)
        {
            // Налаштування стану саги
            InstanceState(x => x.CurrentState);

            Schedule(() => DeliveryTimeoutExpired,
                saga => saga.DeliveryTimeoutTokenId,
                s => {
                    s.Received = r => r.CorrelateById(context => context.Message.CorrelationId);
                    s.Delay = TimeSpan.FromMinutes(5);
                });

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
                        logger.LogInformation("Розпочато сагу доставки повідомлення {MessageId}, CorrelationId: {CorrelationId}",
                            context.Message.MessageId, context.Message.CorrelationId);

                        // Ініціалізація даних саги
                        context.Saga.MessageId = context.Message.MessageId;
                        context.Saga.ChatRoomId = context.Message.ChatRoomId;
                        context.Saga.SenderUserId = context.Message.SenderUserId;
                        context.Saga.CreatedAt = DateTime.UtcNow;
                    })
                    .PublishAsync(context => context.Init<MessageOperationStartCommand>(new
                    {
                        CorrelationId = context.Message.CorrelationId,
                        OperationType = MessageOperationType.SendMessage,
                        UserId = context.Message.SenderUserId,
                        MessageId = context.Message.MessageId,
                        ChatRoomId = context.Message.ChatRoomId,
                        OperationData = new SendMessageDto
                        {
                            ChatRoomId = context.Message.ChatRoomId,
                            Content = context.Message.Content
                        }
                    }))
                    .PublishAsync(context => context.Init<SaveMessageCommand>(new
                    {
                        CorrelationId = context.Message.CorrelationId,
                        MessageId = context.Message.MessageId,
                        ChatRoomId = context.Message.ChatRoomId,
                        SenderUserId = context.Message.SenderUserId,
                        Content = context.Message.Content
                    }))
                    .TransitionTo(SavingMessage)
            );

            // Стан збереження повідомлення
            During(SavingMessage,
                When(MessageSaved)
                    .Then(context =>
                    {
                        logger.LogInformation("Повідомлення {MessageId} успішно збережено", context.Saga.MessageId);

                        context.Saga.IsSaved = true;
                        context.Saga.EncryptedContent = context.Message.EncryptedContent;

                        // Оновлюємо прогрес операції
                        context.Publish(new MessageOperationProgressCommand
                        {
                            CorrelationId = context.Message.CorrelationId,
                            Progress = 50,
                            StatusMessage = "Повідомлення створено"
                        });
                    })
                    .Publish(context => new PublishMessageCommand
                    {
                        CorrelationId = context.Message.CorrelationId,
                        MessageId = context.Message.MessageId,
                        ChatRoomId = context.Saga.ChatRoomId,
                        SenderUserId = context.Saga.SenderUserId,
                        Content = context.Message.EncryptedContent
                    })
                    .TransitionTo(PublishingMessage),

                When(MessageDeliveryFailed)
                    .Then(context =>
                    {
                        logger.LogError("Помилка збереження повідомлення {MessageId}: {ErrorMessage}",
                            context.Saga.MessageId, context.Message.Reason);

                        context.Saga.ErrorMessage = context.Message.Reason;

                        // Позначаємо операцію як невдалу
                        context.Publish(new MessageOperationFailCommand
                        {
                            CorrelationId = context.Saga.CorrelationId,
                            ErrorMessage = context.Message.Reason,
                            ErrorCode = "MESSAGE_CREATION_FAILED"
                        });
                    })
                    .TransitionTo(Failed)
            );

            // Стан публікації повідомлення
            During(PublishingMessage,
                When(MessagePublished)
                    .Then(context =>
                    {
                        logger.LogInformation("Повідомлення {MessageId} опубліковано через SignalR",
                            context.Saga.MessageId);

                        context.Saga.IsPublished = true;
                    })
                    .Schedule(DeliveryTimeoutExpired,
                        context => new MessageDeliveryTimeoutEvent
                        {
                            CorrelationId = context.Message.CorrelationId,
                            MessageId = context.Message.MessageId
                        },
                        context => TimeSpan.FromMinutes(5))
                    .TransitionTo(WaitingDeliveryConfirmation),

                When(MessageDeliveryFailed)
                    .Then(context =>
                    {
                        logger.LogError("Помилка публікації повідомлення {MessageId}: {ErrorMessage}",
                            context.Saga.MessageId, context.Message.Reason);

                        context.Saga.ErrorMessage = context.Message.Reason;

                        // Позначаємо операцію як невдалу
                        context.Publish(new MessageOperationFailCommand
                        {
                            CorrelationId = context.Saga.CorrelationId,
                            ErrorMessage = context.Message.Reason,
                            ErrorCode = "MESSAGE_PUBLISH_FAILED"
                        });
                    })
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

                            logger.LogInformation("Повідомлення {MessageId} доставлено користувачу {UserId}. " +
                                "Всього доставлено {Count} користувачам",
                                context.Saga.MessageId, context.Message.UserId, context.Saga.DeliveredToUserIds.Count);
                        }
                    })
                    .ThenAsync(async context =>
                    {
                        // Відкладене відправлення CheckDeliveryStatusCommand для зменшення навантаження
                        // Чекаємо 200мс перед перевіркою, щоб накопичити більше підтверджень
                        await Task.Delay(200);

                        await context.Publish(new CheckDeliveryStatusCommand
                        {
                            CorrelationId = context.Message.CorrelationId,
                            MessageId = context.Saga.MessageId,
                            ChatRoomId = context.Saga.ChatRoomId,
                            SenderUserId = context.Saga.SenderUserId
                        });
                    }),

                When(DeliveryStatusChecked)
                    .IfElse(context => context.Message.IsDeliveredToAll,
                        // Якщо всі отримали:
                        delivered => delivered
                            .Unschedule(DeliveryTimeoutExpired)
                            .Then(context => {
                                logger.LogInformation("Повідомлення {MessageId} успішно доставлено всім одержувачам",
                                    context.Saga.MessageId);

                                context.Saga.IsDelivered = true;
                                context.Saga.IsDeliveredAfterTimeout = false;
                                context.Saga.ErrorMessage = string.Empty;
                            })
                            .TransitionTo(Completed),
                        // Якщо не всі отримали:
                        notDelivered => notDelivered
                            .Then(context => {
                                logger.LogInformation("Повідомлення {MessageId} доставлено не всім одержувачам. " +
                                    "Продовжуємо очікування", context.Saga.MessageId);
                                // Залишаємося в тому ж стані і продовжуємо чекати
                                // Таймаут спрацює, якщо чекаємо занадто довго
                            })
                    ),

               When(DeliveryTimeoutExpired.Received)
                    .Then(context =>
                    {
                        logger.LogWarning("Таймаут доставки повідомлення {MessageId}. " +
                            "Доставлено {Count} з очікуваних користувачів",
                            context.Saga.MessageId, context.Saga.DeliveredToUserIds.Count);

                        // Помічаємо, що час вийшов, але сага завершується успішно
                        context.Saga.IsDeliveredAfterTimeout = true;
                    })
                    .TransitionTo(Completed),

                When(MessageDeliveryFailed)
                    .Then(context =>
                    {
                        logger.LogError("Помилка доставки повідомлення {MessageId}: {ErrorMessage}",
                            context.Saga.MessageId, context.Message.Reason);

                        context.Saga.ErrorMessage = context.Message.Reason;

                        // Позначаємо операцію як невдалу
                        context.Publish(new MessageOperationFailCommand
                        {
                            CorrelationId = context.Saga.CorrelationId,
                            ErrorMessage = context.Message.Reason,
                            ErrorCode = "OPERATION_TIMEOUT"
                        });
                    })
                    .TransitionTo(Failed)
            );

            // Завершення саги
            WhenEnter(Completed, x => x.Then(context =>
            {
                logger.LogInformation("Сага доставки повідомлення {MessageId} успішно завершена. " +
                    "Доставлено {Count} користувачам, IsDeliveredAfterTimeout: {IsTimeout}",
                    context.Saga.MessageId, context.Saga.DeliveredToUserIds.Count,
                    context.Saga.IsDeliveredAfterTimeout);
            }));

            WhenEnter(Failed, x => x.Then(context =>
            {
                logger.LogError("Сага доставки повідомлення {MessageId} завершена з помилкою: {ErrorMessage}",
                    context.Saga.MessageId, context.Saga.ErrorMessage);
            }));

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
