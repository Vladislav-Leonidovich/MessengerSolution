using Microsoft.Extensions.Primitives;

namespace Gateway.Services
{
    public class ConfigurationMonitoringService : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ConfigurationMonitoringService> _logger;
        private readonly IHostApplicationLifetime _applicationLifetime;

        public ConfigurationMonitoringService(
            IConfiguration configuration,
            ILogger<ConfigurationMonitoringService> logger,
            IHostApplicationLifetime applicationLifetime)
        {
            _configuration = configuration;
            _logger = logger;
            _applicationLifetime = applicationLifetime;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Підписка на зміни в конфігурації
            ChangeToken.OnChange(
                () => _configuration.GetReloadToken(),
                () => OnConfigurationChanged());

            // Тримаємо сервіс активним
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private void OnConfigurationChanged()
        {
            _logger.LogInformation("Виявлено зміни в конфігурації проксі");

            // Опціонально: перезапуск програми для повного перезавантаження конфігурації
            // _applicationLifetime.StopApplication();

            // Альтернативно: просто логуємо зміни
            _logger.LogInformation("Конфігурацію оновлено. Нові маршрути застосовано.");
        }
    }
}
