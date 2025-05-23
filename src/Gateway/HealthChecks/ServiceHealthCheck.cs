using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Gateway.HealthChecks
{
    public class ServiceHealthCheck : IHealthCheck
    {
        private readonly HttpClient _httpClient;
        private readonly string _serviceUrl;
        private readonly string _serviceName;

        public ServiceHealthCheck(string serviceUrl, string serviceName)
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(5)
            };
            _serviceUrl = serviceUrl;
            _serviceName = serviceName;
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
                    return HealthCheckResult.Healthy($"{_serviceName} працює нормально");
                }

                return HealthCheckResult.Degraded(
                    $"{_serviceName} відповідає з кодом {response.StatusCode}");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy(
                    $"{_serviceName} недоступний", ex);
            }
        }
    }
}
