using MassTransit;
using MessageService.Services.Interfaces;
using MessageService.Services;
using MessageService.Sagas.MessageOperation.Consumers;

namespace MessageService.Configuration
{
    public static class MessageOperationServiceExtensions
    {
        /// <summary>
        /// Додає сервіси для роботи з MessageOperation
        /// </summary>
        /// <param name="services">Колекція сервісів</param>
        /// <returns>Колекція сервісів</returns>
        public static IServiceCollection AddMessageOperationServices(this IServiceCollection services)
        {
            // Реєструємо основний сервіс
            services.AddScoped<IMessageOperationService, MessageOperationService>();

            return services;
        }

        /// <summary>
        /// Конфігурує Consumer'и для MessageOperation команд
        /// </summary>
        /// <param name="cfg">Конфігуратор MassTransit</param>
        public static void ConfigureMessageOperationConsumers(this IBusRegistrationConfigurator cfg)
        {
            // Реєструємо Consumer'и для команд MessageOperation
            cfg.AddConsumer<MessageOperationStartCommandConsumer>();
            cfg.AddConsumer<MessageOperationProgressCommandConsumer>();
            cfg.AddConsumer<MessageOperationCompleteCommandConsumer>();
            cfg.AddConsumer<MessageOperationFailCommandConsumer>();
            cfg.AddConsumer<MessageOperationCompensateCommandConsumer>();
        }

        /// <summary>
        /// Конфігурує endpoint'и для MessageOperation Consumer'ів
        /// </summary>
        /// <param name="ctx">Контекст реєстрації шини</param>
        /// <param name="cfg">Конфігуратор endpoint'ів</param>
        public static void ConfigureMessageOperationEndpoints(this IBusRegistrationContext ctx, IRabbitMqBusFactoryConfigurator cfg)
        {
            // Конфігуруємо endpoint'и для Consumer'ів MessageOperation
            cfg.ReceiveEndpoint("message-operation-start", e =>
            {
                e.ConfigureConsumer<MessageOperationStartCommandConsumer>(ctx);
                e.UseMessageRetry(r => r.Intervals(
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(15)
                ));
            });

            cfg.ReceiveEndpoint("message-operation-progress", e =>
            {
                e.ConfigureConsumer<MessageOperationProgressCommandConsumer>(ctx);
                e.UseMessageRetry(r => r.Intervals(
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(3),
                    TimeSpan.FromSeconds(5)
                ));
            });

            cfg.ReceiveEndpoint("message-operation-complete", e =>
            {
                e.ConfigureConsumer<MessageOperationCompleteCommandConsumer>(ctx);
                e.UseMessageRetry(r => r.Intervals(
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(3),
                    TimeSpan.FromSeconds(5)
                ));
            });

            cfg.ReceiveEndpoint("message-operation-fail", e =>
            {
                e.ConfigureConsumer<MessageOperationFailCommandConsumer>(ctx);
                e.UseMessageRetry(r => r.Intervals(
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(3),
                    TimeSpan.FromSeconds(5)
                ));
            });

            cfg.ReceiveEndpoint("message-operation-compensate", e =>
            {
                e.ConfigureConsumer<MessageOperationCompensateCommandConsumer>(ctx);
                e.UseMessageRetry(r => r.Intervals(
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(3),
                    TimeSpan.FromSeconds(5)
                ));
            });
        }
    }
}
