using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core.Interceptors;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Shared.Interceptors
{
    /// <summary>
    /// Серверний interceptor для валідації JWT токенів у gRPC
    /// </summary>
    public class ServerAuthInterceptor : Interceptor
    {
        private readonly ILogger<ServerAuthInterceptor> _logger;

        public ServerAuthInterceptor(ILogger<ServerAuthInterceptor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
            TRequest request,
            ServerCallContext context,
            UnaryServerMethod<TRequest, TResponse> continuation)
        {
            try
            {
                // Перевіряємо наявність токена в metadata
                var authHeader = context.RequestHeaders.FirstOrDefault(h =>
                    h.Key.Equals("authorization", StringComparison.OrdinalIgnoreCase));

                if (authHeader == null)
                {
                    _logger.LogWarning("gRPC запит без токена авторизації від {Peer}", context.Peer);
                    throw new RpcException(new Status(StatusCode.Unauthenticated, "Відсутній токен авторизації"));
                }

                // Токен буде автоматично валідований через ASP.NET Core middleware
                // якщо метод позначений атрибутом [Authorize]

                return await continuation(request, context);
            }
            catch (RpcException)
            {
                throw; // Перекидаємо RpcException без змін
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Непередбачена помилка в ServerAuthInterceptor");
                throw new RpcException(new Status(StatusCode.Internal, "Внутрішня помилка сервера"));
            }
        }
    }
}
