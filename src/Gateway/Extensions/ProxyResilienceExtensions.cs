namespace Gateway.Extensions
{
    public static class ProxyResilienceExtensions
    {
        public static IReverseProxyBuilder AddProxyResiliencePolicies(
            this IReverseProxyBuilder builder)
        {
            // Налаштування HTTP-клієнта для проксі з політиками стійкості
            builder.AddTransforms(transforms =>
            {
                // Додаємо трансформації запитів, якщо потрібно
            });

            // Налаштування HTTP-клієнта
            builder.ConfigureHttpClient((context, handler) =>
            {
                // Базові налаштування для всіх клієнтів
                handler.EnableMultipleHttp2Connections = true;
                handler.PooledConnectionLifetime = TimeSpan.FromMinutes(10);
                handler.MaxConnectionsPerServer = 100;
            });

            return builder;
        }
    }
}
