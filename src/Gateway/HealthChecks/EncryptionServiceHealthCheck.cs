using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Gateway.HealthChecks
{
    public class EncryptionServiceHealthCheck : IHealthCheck
    {
        private readonly HttpClient _httpClient;
        private readonly string _serviceUrl;
        private readonly ILogger<EncryptionServiceHealthCheck> _logger;

        public EncryptionServiceHealthCheck(IConfiguration configuration, ILogger<EncryptionServiceHealthCheck> logger)
        {
            _logger = logger;
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(5)
            };
            _serviceUrl = configuration["ReverseProxy:Clusters:encryptionCluster:Destinations:destination1:Address"];
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
                    return HealthCheckResult.Healthy("Encryption Service працює нормально");
                }

                return HealthCheckResult.Degraded(
                    $"Encryption Service відповідає з кодом {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Encryption Service недоступний");
                return HealthCheckResult.Unhealthy("Encryption Service недоступний", ex);
            }
        }
    }
}
