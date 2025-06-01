using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;

namespace Gateway.Services
{
    public class DynamicProxyConfigProvider : IProxyConfigProvider
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DynamicProxyConfigProvider> _logger;
        private volatile YarpConfig _config;

        public DynamicProxyConfigProvider(
            IConfiguration configuration,
            ILogger<DynamicProxyConfigProvider> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _config = LoadConfig();
        }

        public IProxyConfig GetConfig() => _config;

        public async Task ReloadConfigAsync()
        {
            try
            {
                _config = LoadConfig();
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при перезавантаженні конфігурації проксі");
                throw;
            }
        }

        private YarpConfig LoadConfig()
        {
            var routes = new List<RouteConfig>();
            var clusters = new List<ClusterConfig>();

            // Завантаження маршрутів і кластерів з конфігурації
            var proxySection = _configuration.GetSection("ReverseProxy");

            // Читаємо секції Routes та Clusters з IConfiguration
            var routesSection = proxySection.GetSection("Routes");
            var clustersSection = proxySection.GetSection("Clusters");

            // Парсимо маршрути
            foreach (var routeSection in routesSection.GetChildren())
            {
                var routeId = routeSection.Key;

                // Отримуємо ClusterId для маршруту
                var clusterId = routeSection.GetValue<string>("ClusterId");

                // Отримуємо конфігурацію Match
                var matchSection = routeSection.GetSection("Match");
                var matchConfig = new RouteMatch
                {
                    Path = matchSection.GetValue<string>("Path")
                    // Тут можна додати інші властивості Match (Methods, Headers, тощо)
                };

                // Створюємо об'єкт маршруту
                var route = new RouteConfig
                {
                    RouteId = routeId,
                    ClusterId = clusterId,
                    Match = matchConfig
                    // Можна додати інші налаштування за потреби (Transforms, Metadata, тощо)
                };

                routes.Add(route);
            }

            // Парсимо кластери
            foreach (var clusterSection in clustersSection.GetChildren())
            {
                var clusterId = clusterSection.Key;
                var destinationsSection = clusterSection.GetSection("Destinations");

                var destinations = new Dictionary<string, DestinationConfig>();

                // Парсимо призначення для кластера
                foreach (var destinationSection in destinationsSection.GetChildren())
                {
                    var destinationId = destinationSection.Key;
                    var address = destinationSection.GetValue<string>("Address");

                    destinations[destinationId] = new DestinationConfig
                    {
                        Address = address
                        // Можна додати інші налаштування призначення за потреби
                    };
                }

                // Створюємо об'єкт кластера
                var cluster = new ClusterConfig
                {
                    ClusterId = clusterId,
                    Destinations = destinations
                    // Можна додати інші налаштування кластера за потреби (HttpClient, Metadata, тощо)
                };

                clusters.Add(cluster);
            }

            _logger.LogInformation("Завантажено {RouteCount} маршрутів та {ClusterCount} кластерів",
                routes.Count, clusters.Count);

            return new YarpConfig(routes, clusters);
        }

        // Внутрішній клас для імплементації IProxyConfig
        private class YarpConfig : IProxyConfig
        {
            public YarpConfig(IReadOnlyList<RouteConfig> routes, IReadOnlyList<ClusterConfig> clusters)
            {
                Routes = routes;
                Clusters = clusters;
                ChangeToken = new CancellationChangeToken(CancellationToken.None);
            }

            public IReadOnlyList<RouteConfig> Routes { get; }
            public IReadOnlyList<ClusterConfig> Clusters { get; }
            public IChangeToken ChangeToken { get; }
        }
    }
}
