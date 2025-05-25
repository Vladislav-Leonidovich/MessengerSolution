using ChatService.Services.Interfaces;
using ChatService.Services;
using MassTransit;
using ChatService.Consumers.ChatOperations;

namespace ChatService.Configuration
{
    public static class ChatOperationServiceExtensions
    {
        /// <summary>
        /// Додає сервіси для роботи з ChatOperation
        /// </summary>
        /// <param name="services">Колекція сервісів</param>
        /// <returns>Колекція сервісів</returns>
        public static IServiceCollection AddChatOperationServices(this IServiceCollection services)
        {
            // Реєструємо основний сервіс
            services.AddScoped<IChatOperationService, ChatOperationService>();

            return services;
        }

        /// <summary>
        /// Конфігурує Consumer'и для ChatOperation команд
        /// </summary>
        /// <param name="cfg">Конфігуратор MassTransit</param>
        public static void ConfigureChatOperationConsumers(this IBusRegistrationConfigurator cfg)
        {
            // Реєструємо Consumer'и для команд ChatOperation
            cfg.AddConsumer<ChatOperationStartCommandConsumer>();
            cfg.AddConsumer<ChatOperationProgressCommandConsumer>();
            cfg.AddConsumer<ChatOperationCompleteCommandConsumer>();
            cfg.AddConsumer<ChatOperationFailCommandConsumer>();
            cfg.AddConsumer<ChatOperationCompensateCommandConsumer>();
        }

        /// <summary>
        /// Конфігурує endpoint'и для ChatOperation Consumer'ів
        /// </summary>
        /// <param name="cfg">Конфігуратор endpoint'ів</param>
        public static void ConfigureChatOperationEndpoints(this IBusRegistrationContext ctx, IRabbitMqBusFactoryConfigurator cfg)
        {
            // Конфігуруємо endpoint'и для Consumer'ів ChatOperation
            cfg.ReceiveEndpoint("chat-operation-start", e =>
            {
                e.ConfigureConsumer<ChatOperationStartCommandConsumer>(ctx);
                e.UseMessageRetry(r => r.Intervals(
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(15)
                ));
            });

            cfg.ReceiveEndpoint("chat-operation-progress", e =>
            {
                e.ConfigureConsumer<ChatOperationProgressCommandConsumer>(ctx);
                e.UseMessageRetry(r => r.Intervals(
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(5)
                ));
            });

            cfg.ReceiveEndpoint("chat-operation-complete", e =>
            {
                e.ConfigureConsumer<ChatOperationCompleteCommandConsumer>(ctx);
                e.UseMessageRetry(r => r.Intervals(
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(15)
                ));
            });

            cfg.ReceiveEndpoint("chat-operation-fail", e =>
            {
                e.ConfigureConsumer<ChatOperationFailCommandConsumer>(ctx);
                e.UseMessageRetry(r => r.Intervals(
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(5)
                ));
            });

            cfg.ReceiveEndpoint("chat-operation-compensate", e =>
            {
                e.ConfigureConsumer<ChatOperationCompensateCommandConsumer>(ctx);
                e.UseMessageRetry(r => r.Intervals(
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(15)
                ));
            });
        }
    }
}
