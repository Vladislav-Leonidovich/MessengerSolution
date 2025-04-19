using Grpc.Net.Client;
using Shared.Protos;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Polly;
using Polly.Retry;
using Polly.CircuitBreaker;
using System.Net.Http;
using MessageService.Services.Interfaces;
using Grpc.Core;
using Shared.Exceptions;

namespace MessageService.Services
{
    public class EncryptionGrpcClient : IEncryptionGrpcClient
    {
        private readonly GrpcChannel _channel;
        private readonly EncryptionGrpcService.EncryptionGrpcServiceClient _client;
        private readonly ILogger<EncryptionGrpcClient> _logger;
        private readonly AsyncRetryPolicy _retryPolicy;
        private readonly AsyncCircuitBreakerPolicy _circuitBreakerPolicy;
        private readonly IAsyncPolicy _resiliencePolicy;

        public EncryptionGrpcClient(IConfiguration configuration, ILogger<EncryptionGrpcClient> logger)
        {
            _logger = logger;

            // Настройка канала gRPC
            var encryptionServiceUrl = configuration["Services:EncryptionService:GrpcUrl"];
            _channel = GrpcChannel.ForAddress(encryptionServiceUrl, new GrpcChannelOptions
            {
                HttpHandler = new SocketsHttpHandler
                {
                    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
                    KeepAlivePingDelay = TimeSpan.FromSeconds(60),
                    KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
                    EnableMultipleHttp2Connections = true
                }
            });

            _client = new EncryptionGrpcService.EncryptionGrpcServiceClient(_channel);

            // Настройка Polly для отказоустойчивости
            _retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    3,
                    retryAttempt => TimeSpan.FromMilliseconds(100 * Math.Pow(2, retryAttempt)),
                    onRetry: (ex, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning(ex,
                            "Помилка під час виклику gRPC-сервісу шифрування. Повторна спроба {RetryCount} через {RetryInterval}ms",
                            retryCount, timeSpan.TotalMilliseconds);
                    });

            _circuitBreakerPolicy = Policy
                .Handle<Exception>()
                .CircuitBreakerAsync(
                    5,
                    TimeSpan.FromMinutes(1),
                    onBreak: (ex, breakDelay) =>
                    {
                        _logger.LogError(ex,
                            "Перевищено кількість помилок у сервісі шифрування. Circuit breaker відкрито на {BreakDelay}ms",
                            breakDelay.TotalMilliseconds);
                    },
                    onReset: () =>
                    {
                        _logger.LogInformation("Автоматичний вимикач закрито. Відновлено з'єднання з сервісом шифрування");
                    });

            _resiliencePolicy = Policy.WrapAsync(_retryPolicy, _circuitBreakerPolicy);
        }

        public async Task<string> EncryptAsync(string plainText)
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    var response = await _client.EncryptAsync(new EncryptRequest
                    {
                        PlainText = plainText
                    });

                    if (!response.Success)
                    {
                        throw new Exception($"Помилка шифрування: {response.ErrorMessage}");
                    }

                    return response.CipherText;
                });
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
            {
                _logger.LogError(ex, "Сервіс шифрування недоступний");
                throw new ServiceUnavailableException("Сервіс шифрування тимчасово недоступний", ex);
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Unauthenticated)
            {
                _logger.LogError(ex, "Помилка аутентифікації під час виклику сервісу шифрування");
                throw new ForbiddenAccessException("Помилка аутентифікації під час доступу до сервісу шифрування");
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, "gRPC помилка під час шифрування: {StatusCode}", ex.StatusCode);
                throw new ApplicationException($"Помилка обробки запиту: {ex.Status.Detail}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Несподівана помилка під час шифрування");
                throw;
            }
        }

        public async Task<string> DecryptAsync(string cipherText)
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    var response = await _client.DecryptAsync(new DecryptRequest
                    {
                        CipherText = cipherText
                    });

                    if (!response.Success)
                    {
                        throw new Exception($"Помилка дешифрування: {response.ErrorMessage}");
                    }

                    return response.PlainText;
                });
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
            {
                _logger.LogError(ex, "Сервіс шифрування недоступний");
                throw new ServiceUnavailableException("Сервіс шифрування тимчасово недоступний", ex);
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Unauthenticated)
            {
                _logger.LogError(ex, "Помилка аутентифікації під час виклику сервісу шифрування");
                throw new ForbiddenAccessException("Помилка аутентифікації під час доступу до сервісу шифрування");
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, "gRPC помилка під час шифрування: {StatusCode}", ex.StatusCode);
                throw new ApplicationException($"Помилка обробки запиту: {ex.Status.Detail}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Несподівана помилка під час дешифрування");
                throw;
            }
        }

        public async Task<List<string>> EncryptBatchAsync(List<string> plainTexts)
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    var request = new BatchEncryptRequest();
                    request.PlainTexts.AddRange(plainTexts);

                    var response = await _client.EncryptBatchAsync(request);

                    if (!response.Success)
                    {
                        throw new Exception($"Помилка пакетного шифрування: {response.ErrorMessage}");
                    }

                    return response.CipherTexts.ToList();
                });
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
            {
                _logger.LogError(ex, "Сервіс шифрування недоступний");
                throw new ServiceUnavailableException("Сервіс шифрування тимчасово недоступний", ex);
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Unauthenticated)
            {
                _logger.LogError(ex, "Помилка аутентифікації під час виклику сервісу шифрування");
                throw new ForbiddenAccessException("Помилка аутентифікації під час доступу до сервісу шифрування");
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, "gRPC помилка під час шифрування: {StatusCode}", ex.StatusCode);
                throw new ApplicationException($"Помилка обробки запиту: {ex.Status.Detail}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Несподівана помилка під час пакетного шифрування");
                throw;
            }
        }

        public async Task<List<string>> DecryptBatchAsync(List<string> cipherTexts)
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    var request = new BatchDecryptRequest();
                    request.CipherTexts.AddRange(cipherTexts);

                    var response = await _client.DecryptBatchAsync(request);

                    if (!response.Success)
                    {
                        throw new Exception($"Помилка пакетного дешифрування: {response.ErrorMessage}");
                    }

                    return response.PlainTexts.ToList();
                });
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
            {
                _logger.LogError(ex, "Сервіс шифрування недоступний");
                throw new ServiceUnavailableException("Сервіс шифрування тимчасово недоступний", ex);
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Unauthenticated)
            {
                _logger.LogError(ex, "Помилка аутентифікації під час виклику сервісу шифрування");
                throw new ForbiddenAccessException("Помилка аутентифікації під час доступу до сервісу шифрування");
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, "gRPC помилка під час шифрування: {StatusCode}", ex.StatusCode);
                throw new ApplicationException($"Помилка обробки запиту: {ex.Status.Detail}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Несподівана помилка під час пакетного дешифрування");
                throw;
            }
        }

        public void Dispose()
        {
            _channel?.Dispose();
        }
    }
}
