using System.Reflection;
using MassTransit;
using MessageService.Data;
using MessageService.Sagas.DeleteAllMessages;
using MessageService.Sagas.DeleteAllMessages.Consumers;
using MessageService.Sagas.MessageDelivery;
using MessageService.Sagas.MessageDelivery.Consumers;

namespace MessageService.Configuration
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

                // Реєстрація споживачів для саги видалення повідомлень
                busConfigurator.AddConsumer<DeleteChatMessagesCommandConsumer>();
                busConfigurator.AddConsumer<SendChatNotificationCommandConsumer>();

                // Реєстрація споживачів для саги доставки повідомлень
                busConfigurator.AddConsumer<SaveMessageCommandConsumer>();
                busConfigurator.AddConsumer<PublishMessageCommandConsumer>();
                busConfigurator.AddConsumer<CheckDeliveryStatusCommandConsumer>();
                busConfigurator.AddConsumer<MessageDeliveredToUserEventConsumer>();
                busConfigurator.AddConsumer<MessageStatusUpdateConsumer>();

                // Реєстрація State Machine Saga
                busConfigurator.AddSagaStateMachine<MessageDeliverySagaStateMachine, MessageDeliverySagaState>()
                    .EntityFrameworkRepository(r =>
                    {
                        r.ExistingDbContext<MessageDeliverySagaDbContext>();
                        r.ConcurrencyMode = ConcurrencyMode.Pessimistic;
                    });

                busConfigurator.AddSagaStateMachine<DeleteAllMessagesSagaStateMachine, DeleteAllMessagesSagaState>()
                    .EntityFrameworkRepository(r =>
                    {
                        r.ExistingDbContext<MessageDbContext>();
                        r.ConcurrencyMode = ConcurrencyMode.Pessimistic;
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
            // Saga endpoints
            ConfigureSagaEndpoints(context, configurator);
        }

        /// <summary>
        /// Налаштовує endpoint'и для Saga Consumer'ів
        /// </summary>
        private static void ConfigureSagaEndpoints(IBusRegistrationContext context, IRabbitMqBusFactoryConfigurator configurator)
        {
            configurator.ReceiveEndpoint("delete-chat-messages", e =>
            {
                e.ConfigureConsumer<DeleteChatMessagesCommandConsumer>(context);
                e.UseMessageRetry(r => r.Intervals(
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(15)
                ));
            });

            configurator.ReceiveEndpoint("send-chat-notification", e =>
            {
                e.ConfigureConsumer<SendChatNotificationCommandConsumer>(context);
                e.UseMessageRetry(r => r.Intervals(
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(5)
                ));
            });

            configurator.ReceiveEndpoint("save-message", e =>
            {
                e.ConfigureConsumer<SaveMessageCommandConsumer>(context);
                e.UseMessageRetry(r => r.Intervals(
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(15)
                ));
            });

            configurator.ReceiveEndpoint("publish-message", e =>
            {
                e.ConfigureConsumer<PublishMessageCommandConsumer>(context);
                e.UseMessageRetry(r => r.Intervals(
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(5)
                ));
            });

            configurator.ReceiveEndpoint("check-delivery-status", e =>
            {
                e.ConfigureConsumer<CheckDeliveryStatusCommandConsumer>(context);
                e.UseMessageRetry(r => r.Intervals(
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(15)
                ));
            });
        }
    }
}
