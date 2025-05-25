using Yarp.ReverseProxy.Transforms;

namespace Gateway.Configuration
{
    public static class ProxyPoliciesConfig
    {
        public static IReverseProxyBuilder AddProxyResiliencePolicies(
            this IReverseProxyBuilder builder)
        {
            // Додаємо загальні налаштування для всіх маршрутів
            return builder.AddTransforms(transformBuilder =>
            {
                // Додаємо таймаути
                transformBuilder.AddRequestTransform(async context =>
                {
                    context.ProxyRequest.VersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
                    context.HttpContext.Request.EnableBuffering();
                });
            })
            // Видалено некоректний виклик .AddHealthChecks()
            .ConfigureHttpClient((context, handler) =>
            {
                // Налаштування Client-side circuit breaker
                handler.EnableMultipleHttp2Connections = true;
                handler.PooledConnectionLifetime = TimeSpan.FromMinutes(10);
                handler.MaxConnectionsPerServer = 100;
            });
        }
    }
}
