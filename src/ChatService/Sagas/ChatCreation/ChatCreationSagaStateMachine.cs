using System.Text.Json;
using ChatService.Sagas.ChatCreation.Events;
using ChatService.Sagas.ChatOperation.Events;
using ChatService.Services.Interfaces;
using MassTransit;
using Shared.DTOs.Chat;

namespace ChatService.Sagas.ChatCreation
{
    public class ChatCreationSagaStateMachine : MassTransitStateMachine<ChatCreationSagaState>
    {
        public ChatCreationSagaStateMachine()
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
            Event(() => ChatCreationStarted, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => ChatRoomCreated, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => MessageServiceNotified, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => FailureOccurred, x => x.CorrelateById(context => context.Message.CorrelationId));

            // Початковий стан - створення операції відстеження
            Initially(
                When(ChatCreationStarted)
                    .Then(context =>
                    {
                        context.Saga.ChatRoomId = context.Message.ChatRoomId;
                        context.Saga.CreatorUserId = context.Message.CreatorUserId;
                        context.Saga.MemberIdsJson = JsonSerializer.Serialize(context.Message.MemberIds);
                        context.Saga.CreatedAt = DateTime.UtcNow;
                        context.Saga.StartedAt = DateTime.UtcNow;
                        context.Saga.Progress = 0;
                        context.Saga.StatusMessage = "Початок створення чату";
                        context.Saga.LastUpdatedAt = DateTime.UtcNow;
                    })
                    // Створюємо операцію відстеження
                    .PublishAsync(context => context.Init<ChatOperationStartCommand>(new
                    {
                        CorrelationId = context.Message.CorrelationId,
                        OperationType = ChatOperationType.Create,
                        ChatRoomId = context.Message.ChatRoomId,
                        UserId = context.Message.CreatorUserId,
                        OperationData = JsonSerializer.Serialize(new { MemberIds = context.Message.MemberIds })
                    }))
                    .Schedule(OperationTimeout,
                        context => new ChatCreationSagaTimeoutEvent
                        {
                            CorrelationId = context.Message.CorrelationId,
                            TimeoutReason = "Timeout waiting for chat creation"
                        })
                    .PublishAsync(context => context.Init<CreateChatRoomCommand> (new
                    {
                        CorrelationId = context.Message.CorrelationId,
                        ChatRoomId = context.Message.ChatRoomId,
                        CreatorUserId = context.Message.CreatorUserId,
                        MemberIds = context.Message.MemberIds
                    }))
                    .TransitionTo(CreatingChatRoom)
            );

            // Стан створення чат-кімнати
            During(CreatingChatRoom,
                When(ChatRoomCreated)
                    .Then(context =>
                    {
                        // Оновлюємо прогрес операції
                        context.Publish(new ChatOperationProgressCommand
                        {
                            CorrelationId = context.Message.CorrelationId,
                            Progress = 50,
                            StatusMessage = "Чат-кімнату створено, повідомляємо MessageService"
                        });
                    })
                    .Publish(context => new NotifyMessageServiceCommand
                    {
                        CorrelationId = context.Message.CorrelationId,
                        ChatRoomId = context.Message.ChatRoomId
                    })
                    .TransitionTo(NotifyingMessageService),

                When(FailureOccurred)
                    .Then(context =>
                    {
                        context.Saga.ErrorMessage = context.Message.Reason;
                        // Позначаємо операцію як невдалу
                        context.Publish(new ChatOperationFailCommand
                        {
                            CorrelationId = context.Saga.CorrelationId,
                            ErrorMessage = context.Message.Reason,
                            ErrorCode = "CHAT_CREATION_FAILED"
                        });
                    })
                    .Unschedule(OperationTimeout)
                    .Publish(context => new CompensateChatCreationCommand
                    {
                        CorrelationId = context.Saga.CorrelationId,
                        ChatRoomId = context.Saga.ChatRoomId,
                        Reason = context.Message.Reason
                    })
                    .TransitionTo(Compensating)
            );

            // Стан повідомлення MessageService
            During(NotifyingMessageService,
                When(MessageServiceNotified)
                    .Then(context =>
                    {
                        // Завершуємо операцію успішно
                        context.Publish(new ChatOperationCompleteCommand
                        {
                            CorrelationId = context.Message.CorrelationId,
                            Result = JsonSerializer.Serialize(new { ChatRoomId = context.Message.ChatRoomId })
                        });
                    })
                    .Unschedule(OperationTimeout)
                    .Publish(context => new CompleteChatCreationCommand
                    {
                        CorrelationId = context.Message.CorrelationId,
                        ChatRoomId = context.Message.ChatRoomId
                    })
                    .TransitionTo(Completed),

                When(FailureOccurred)
                    .Then(context =>
                    {
                        context.Saga.ErrorMessage = context.Message.Reason;
                        // Позначаємо операцію як невдалу
                        context.Publish(new ChatOperationFailCommand
                        {
                            CorrelationId = context.Saga.CorrelationId,
                            ErrorMessage = context.Message.Reason,
                            ErrorCode = "MESSAGE_SERVICE_NOTIFICATION_FAILED"
                        });
                    })
                    .Unschedule(OperationTimeout)
                    .Publish(context => new CompensateChatCreationCommand
                    {
                        CorrelationId = context.Saga.CorrelationId,
                        ChatRoomId = context.Saga.ChatRoomId,
                        Reason = context.Message.Reason
                    })
                    .TransitionTo(Compensating)
            );

            // Стан компенсації
            During(Compensating,
                When(ChatCreationCompensated)
                    .Then(context =>
                    {
                        // Позначаємо операцію як компенсовану
                        context.Publish(new ChatOperationCompensateCommand
                        {
                            CorrelationId = context.Saga.CorrelationId,
                            Reason = context.Message.Reason
                        });
                    })
                    .TransitionTo(Failed)
            );

            // Обробка таймауту
            During(CreatingChatRoom, NotifyingMessageService,
                When(OperationTimeout.Received)
                    .Then(context =>
                    {
                        context.Saga.ErrorMessage = "Операція перевищила ліміт часу";
                        // Позначаємо операцію як невдалу через таймаут
                        context.Publish(new ChatOperationFailCommand
                        {
                            CorrelationId = context.Message.CorrelationId,
                            ErrorMessage = context.Message.TimeoutReason,
                            ErrorCode = "OPERATION_TIMEOUT"
                        });
                    })
                    .Publish(context => new CompensateChatCreationCommand
                    {
                        CorrelationId = context.Saga.CorrelationId,
                        ChatRoomId = context.Saga.ChatRoomId,
                        Reason = "Операція перевищила ліміт часу"
                    })
                    .TransitionTo(Compensating)
            );

            // Автоматичне завершення саги
            SetCompletedWhenFinalized();
        }

        // Стани
        public State? CreatingChatRoom { get; private set; }
        public State? NotifyingMessageService { get; private set; }
        public State? Completed { get; private set; }
        public State? Compensating { get; private set; }
        public State? Failed { get; private set; }

        // Події
        public Event<ChatCreationStartedEvent>? ChatCreationStarted { get; private set; }
        public Event<ChatRoomCreatedEvent>? ChatRoomCreated { get; private set; }
        public Event<MessageServiceNotifiedEvent>? MessageServiceNotified { get; private set; }
        public Event<ChatCreationFailedEvent>? FailureOccurred { get; private set; }
        public Event<ChatCreationCompensatedEvent>? ChatCreationCompensated { get; private set; }

        // Розклад таймауту
        public Schedule<ChatCreationSagaState, ChatCreationSagaTimeoutEvent>? OperationTimeout { get; private set; }
    }
}
