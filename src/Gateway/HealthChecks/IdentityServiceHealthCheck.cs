using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Gateway.HealthChecks
{
    public class IdentityServiceHealthCheck : IHealthCheck
    {
        private readonly HttpClient _httpClient;
        private readonly string _serviceUrl;
        private readonly ILogger<IdentityServiceHealthCheck> _logger;

        public IdentityServiceHealthCheck(IConfiguration configuration, ILogger<IdentityServiceHealthCheck> logger)
        {
            _logger = logger;
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(5)
            };
            _serviceUrl = configuration["ReverseProxy:Clusters:identityCluster:Destinations:destination1:Address"];
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
                    return HealthCheckResult.Healthy("Identity Service працює нормально");
                }

                return HealthCheckResult.Degraded(
                    $"Identity Service відповідає з кодом {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Identity Service недоступний");
                return HealthCheckResult.Unhealthy("Identity Service недоступний", ex);
            }
        }
    }
}
