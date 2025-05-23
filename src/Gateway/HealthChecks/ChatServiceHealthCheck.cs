using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Gateway.HealthChecks
{
    public class ChatServiceHealthCheck : IHealthCheck
    {
        private readonly HttpClient _httpClient;
        private readonly string _serviceUrl;
        private readonly ILogger<ChatServiceHealthCheck> _logger;

        public ChatServiceHealthCheck(IConfiguration configuration, ILogger<ChatServiceHealthCheck> logger)
        {
            _logger = logger;
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(5)
            };
            _serviceUrl = configuration["ReverseProxy:Clusters:chatCluster:Destinations:destination1:Address"];
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_serviceUrl}/api/health", cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return HealthCheckResult.Healthy("Chat Service працює нормально");
                }

                return HealthCheckResult.Degraded(
                    $"Chat Service відповідає з кодом {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Chat Service недоступний");
                return HealthCheckResult.Unhealthy("Chat Service недоступний", ex);
            }
        }
    }
}
