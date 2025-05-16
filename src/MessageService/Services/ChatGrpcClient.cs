using Grpc.Net.Client;
using Polly;
using Shared.Protos;
using MessageService.Services.Interfaces;
using MessageServiceDTOs;

namespace MessageService.Services
{
    // src/MessageService/Services/ChatGrpcClient.cs
    public class ChatGrpcClient : IChatGrpcClient, IDisposable
    {
        private readonly GrpcChannel _channel;
        private readonly ChatAuthorizationService.ChatAuthorizationServiceClient _client;
        private readonly ILogger<ChatGrpcClient> _logger;
        private readonly IAsyncPolicy _resiliencePolicy;

        public ChatGrpcClient(
            IConfiguration config,
            ILogger<ChatGrpcClient> logger)
        {
            _logger = logger;

            // Настройка канала
            var chatServiceUrl = config["Services:ChatService:GrpcUrl"];
            _channel = GrpcChannel.ForAddress(chatServiceUrl, new GrpcChannelOptions
            {
                HttpHandler = new SocketsHttpHandler
                {
                    EnableMultipleHttp2Connections = true,
                    KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
                    KeepAlivePingDelay = TimeSpan.FromSeconds(60),
                    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5)
                }
            });

            _client = new ChatAuthorizationService.ChatAuthorizationServiceClient(_channel);

            // Настройка политики отказоустойчивости
            _resiliencePolicy = CreateResiliencePolicy();
        }

        public async Task<bool> CheckAccessAsync(int userId, int chatRoomId)
        {
            try
            {
                var request = new CheckAccessRequest
                {
                    UserId = userId,
                    ChatRoomId = chatRoomId
                };

                var response = await _resiliencePolicy.ExecuteAsync(async () =>
                    await _client.CheckAccessAsync(request));

                return response.HasAccess;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при перевірці доступу користувача {UserId} до чату {ChatRoomId}",
                    userId, chatRoomId);
                return false;
            }
        }

        public async Task<Dictionary<(int UserId, int ChatRoomId), bool>> CheckAccessBatchAsync(
            List<(int UserId, int ChatRoomId)> checks)
        {
            try
            {
                var request = new CheckAccessBatchRequest();
                foreach (var (userId, chatRoomId) in checks)
                {
                    request.Checks.Add(new CheckAccessRequest
                    {
                        UserId = userId,
                        ChatRoomId = chatRoomId
                    });
                }

                var response = await _resiliencePolicy.ExecuteAsync(async () =>
                    await _client.CheckAccessBatchAsync(request));

                var result = new Dictionary<(int UserId, int ChatRoomId), bool>();
                foreach (var item in response.Results)
                {
                    result[(item.UserId, item.ChatRoomId)] = item.HasAccess;
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при пакетній перевірці доступу до чатів");
                return checks.ToDictionary(key => key, _ => false);
            }
        }

        public async Task<List<int>> GetChatParticipantsAsync(int chatRoomId)
        {
            try
            {
                var request = new GetChatParticipantsRequest
                {
                    ChatRoomId = chatRoomId
                };

                var response = await _resiliencePolicy.ExecuteAsync(async () =>
                    await _client.GetChatParticipantsAsync(request));

                if (!response.Success)
                {
                    _logger.LogWarning("Помилка отримання учасників чату {ChatRoomId}: {ErrorMessage}",
                        chatRoomId, response.ErrorMessage);
                    return new List<int>();
                }

                return response.ParticipantIds.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при отриманні учасників чату {ChatRoomId}", chatRoomId);
                return new List<int>();
            }
        }

        /*public async Task<bool> CheckAdminAccessAsync(int userId, int chatRoomId)
        {
            try
            {
                // Якщо у вас є окремий метод у gRPC для перевірки прав адміністратора,
                // використовуйте його. В іншому випадку можна імплементувати через
                // перевірку ролі користувача в чаті.

                var request = new CheckAdminAccessRequest
                {
                    UserId = userId,
                    ChatRoomId = chatRoomId
                };

                var response = await _resiliencePolicy.ExecuteAsync(async () =>
                    await _client.CheckAdminAccessAsync(request));

                return response.IsAdmin;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при перевірці прав адміністратора користувача {UserId} у чаті {ChatRoomId}",
                    userId, chatRoomId);
                return false;
            }
        }*/

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

        public void Dispose()
        {
            _channel?.Dispose();
        }
    }
}
