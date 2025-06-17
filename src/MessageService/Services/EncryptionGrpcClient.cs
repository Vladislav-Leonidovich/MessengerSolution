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
using Microsoft.AspNetCore.Authorization;

namespace MessageService.Services
{
    public class EncryptionGrpcClient : IEncryptionGrpcClient
    {
        private readonly EncryptionGrpcService.EncryptionGrpcServiceClient _client;
        private readonly ILogger<EncryptionGrpcClient> _logger;
        private readonly IAsyncPolicy _resiliencePolicy;

        public EncryptionGrpcClient(
            EncryptionGrpcService.EncryptionGrpcServiceClient client, 
            ILogger<EncryptionGrpcClient> logger)
        {
            _client = client;
            _logger = logger;

            // Настройка политики отказоустойчивости
            _resiliencePolicy = CreateResiliencePolicy();

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

        private IAsyncPolicy CreateResiliencePolicy()
        {
            var retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    3, // количество повторов
                    retryAttempt => TimeSpan.FromMilliseconds(100 * Math.Pow(2, retryAttempt)), // экспоненциальная задержка
                    onRetry: (ex, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning(ex,
                            "Помилка під час виклику gRPC-сервісу чатів. Повторна спроба {RetryCount} через {RetryInterval}мс",
                            retryCount, timeSpan.TotalMilliseconds);
                    });

            var circuitBreakerPolicy = Policy
                .Handle<Exception>()
                .CircuitBreakerAsync(
                    5, // количество сбоев, после которых сработает circuit breaker
                    TimeSpan.FromMinutes(1), // время, на которое circuit breaker разомкнет цепь
                    onBreak: (ex, breakDelay) =>
                    {
                        _logger.LogError(ex,
                            "Перевищено кількість помилок у сервісі чатів. Circuit breaker відкрито на {BreakDelay}мс",
                            breakDelay.TotalMilliseconds);
                    },
                    onReset: () =>
                    {
                        _logger.LogInformation("Circuit breaker закрито. Відновлено з'єднання з сервісом чатів");
                    });

            // Объединяем политики
            return Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);
        }
    }
}
