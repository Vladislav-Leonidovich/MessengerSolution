using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Gateway.HealthChecks
{
    public class MessageServiceHealthCheck : IHealthCheck
    {
        private readonly HttpClient _httpClient;
        private readonly string _serviceUrl;
        private readonly ILogger<MessageServiceHealthCheck> _logger;

        public MessageServiceHealthCheck(IConfiguration configuration, ILogger<MessageServiceHealthCheck> logger)
        {
            _logger = logger;
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(5)
            };
            _serviceUrl = configuration["ReverseProxy:Clusters:messageCluster:Destinations:destination1:Address"];
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
                    return HealthCheckResult.Healthy("Message Service працює нормально");
                }

                return HealthCheckResult.Degraded(
                    $"Message Service відповідає з кодом {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Message Service недоступний");
                return HealthCheckResult.Unhealthy("Message Service недоступний", ex);
            }
        }
    }
}
