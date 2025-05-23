using Yarp.ReverseProxy.Configuration;

namespace Gateway.Services
{
    public class DynamicProxyConfigProvider : IProxyConfigProvider
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DynamicProxyConfigProvider> _logger;
        private volatile ProxyConfig _config;

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

        private ProxyConfig LoadConfig()
        {
            var routes = new List<RouteConfig>();
            var clusters = new List<ClusterConfig>();

            // Завантаження маршрутів і кластерів з конфігурації
            var proxySection = _configuration.GetSection("ReverseProxy");

            // Читаємо секції Routes та Clusters з IConfiguration
            var routesSection = proxySection.GetSection("Routes");
            var clustersSection = proxySection.GetSection("Clusters");

            // Перетворюємо налаштування у відповідні конфігураційні об'єкти
            // ... (код для парсингу конфігурації)

            return new ProxyConfig(routes, clusters);
        }
    }
}
