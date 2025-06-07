using System.Reflection;
using ChatService.Consumers.ChatOperations;
using ChatService.Sagas.ChatCreation;
using ChatService.Sagas.ChatCreation.Consumers;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Configuration
{
    public static class MassTransitExtensions
    {
        /// <summary>
        /// Додає налаштування MassTransit з RabbitMQ
        /// </summary>
        /// <param name="services">Колекція сервісів</param>
        /// <param name="configuration">Конфігурація застосунку</param>
        /// <returns>Колекція сервісів</returns>
        public static IServiceCollection AddMassTransitConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddMassTransit(busConfigurator =>
            {
                // Автоматична реєстрація всіх Consumer'ів з поточної збірки
                busConfigurator.AddConsumers(Assembly.GetExecutingAssembly());

                // Реєстрація конкретних Consumer'ів для ChatOperation
                busConfigurator.AddConsumer<ChatOperationStartCommandConsumer>();
                busConfigurator.AddConsumer<ChatOperationProgressCommandConsumer>();
                busConfigurator.AddConsumer<ChatOperationCompleteCommandConsumer>();
                busConfigurator.AddConsumer<ChatOperationFailCommandConsumer>();
                busConfigurator.AddConsumer<ChatOperationCompensateCommandConsumer>();

                // Реєстрація Consumer'ів для Saga
                busConfigurator.AddConsumer<CreateChatRoomCommandConsumer>();
                busConfigurator.AddConsumer<NotifyMessageServiceCommandConsumer>();
                busConfigurator.AddConsumer<CompensateChatCreationCommandConsumer>();
                busConfigurator.AddConsumer<CompleteChatCreationCommandConsumer>();

                // Реєстрація State Machine Saga
                busConfigurator.AddSagaStateMachine<ChatCreationSagaStateMachine, ChatCreationSagaState>()
                    .EntityFrameworkRepository(r =>
                    {
                        r.ConcurrencyMode = ConcurrencyMode.Pessimistic;
                        r.AddDbContext<DbContext, ChatService.Data.ChatDbContext>();
                    });

                // Налаштування RabbitMQ
                busConfigurator.UsingRabbitMq((context, configurator) =>
                {
                    var rabbitMqHost = configuration["RabbitMQ:Host"] ?? "localhost";
                    var rabbitMqPort = configuration.GetValue<int>("RabbitMQ:Port", 5672);
                    var rabbitMqUsername = configuration["RabbitMQ:Username"] ?? "guest";
                    var rabbitMqPassword = configuration["RabbitMQ:Password"] ?? "guest";
                    var rabbitMqVirtualHost = configuration["RabbitMQ:VirtualHost"] ?? "/";

                    configurator.Host(rabbitMqHost, hostConfigurator =>
                    {
                        hostConfigurator.Username(rabbitMqUsername);
                        hostConfigurator.Password(rabbitMqPassword);
                    });

                    // Налаштування черг для Consumer'ів
                    ConfigureEndpoints(context, configurator);

                    // Загальні налаштування
                    configurator.ConfigureEndpoints(context);
                });
            });

            return services;
        }

        /// <summary>
        /// Налаштовує endpoint'и для Consumer'ів
        /// </summary>
        /// <param name="context">Контекст реєстрації шини</param>
        /// <param name="configurator">Конфігуратор RabbitMQ</param>
        private static void ConfigureEndpoints(IBusRegistrationContext context, IRabbitMqBusFactoryConfigurator configurator)
        {
            // ChatOperation endpoints
            ConfigureChatOperationEndpoints(context, configurator);

            // Saga endpoints
            ConfigureSagaEndpoints(context, configurator);
        }

        /// <summary>
        /// Налаштовує endpoint'и для ChatOperation Consumer'ів
        /// </summary>
        private static void ConfigureChatOperationEndpoints(IBusRegistrationContext context, IRabbitMqBusFactoryConfigurator configurator)
        {
            configurator.ReceiveEndpoint("chat-operation-start", e =>
            {
                e.ConfigureConsumer<ChatOperationStartCommandConsumer>(context);
                e.UseMessageRetry(r => r.Intervals(
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(15)
                ));
            });

            configurator.ReceiveEndpoint("chat-operation-progress", e =>
            {
                e.ConfigureConsumer<ChatOperationProgressCommandConsumer>(context);
                e.UseMessageRetry(r => r.Intervals(
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(5)
                ));
            });

            configurator.ReceiveEndpoint("chat-operation-complete", e =>
            {
                e.ConfigureConsumer<ChatOperationCompleteCommandConsumer>(context);
                e.UseMessageRetry(r => r.Intervals(
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(15)
                ));
            });

            configurator.ReceiveEndpoint("chat-operation-fail", e =>
            {
                e.ConfigureConsumer<ChatOperationFailCommandConsumer>(context);
                e.UseMessageRetry(r => r.Intervals(
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(5)
                ));
            });

            configurator.ReceiveEndpoint("chat-operation-compensate", e =>
            {
                e.ConfigureConsumer<ChatOperationCompensateCommandConsumer>(context);
                e.UseMessageRetry(r => r.Intervals(
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(15)
                ));
            });
        }

        /// <summary>
        /// Налаштовує endpoint'и для Saga Consumer'ів
        /// </summary>
        private static void ConfigureSagaEndpoints(IBusRegistrationContext context, IRabbitMqBusFactoryConfigurator configurator)
        {
            configurator.ReceiveEndpoint("create-chat-room", e =>
            {
                e.ConfigureConsumer<CreateChatRoomCommandConsumer>(context);
                e.UseMessageRetry(r => r.Intervals(
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(15),
                    TimeSpan.FromSeconds(30)
                ));
            });

            configurator.ReceiveEndpoint("notify-message-service", e =>
            {
                e.ConfigureConsumer<NotifyMessageServiceCommandConsumer>(context);
                e.UseMessageRetry(r => r.Intervals(
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(15)
                ));
            });

            configurator.ReceiveEndpoint("compensate-chat-creation", e =>
            {
                e.ConfigureConsumer<CompensateChatCreationCommandConsumer>(context);
                e.UseMessageRetry(r => r.Intervals(
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(5)
                ));
            });

            configurator.ReceiveEndpoint("complete-chat-creation", e =>
            {
                e.ConfigureConsumer<CompleteChatCreationCommandConsumer>(context);
                e.UseMessageRetry(r => r.Intervals(
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(5)
                ));
            });
        }
    }
}
