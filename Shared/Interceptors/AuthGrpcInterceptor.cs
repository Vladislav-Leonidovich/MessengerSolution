using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace Shared.Interceptors
{
    public class AuthGrpcInterceptor : Interceptor
    {
        private readonly Func<Task<string>> _tokenProvider;
        private readonly ILogger<AuthGrpcInterceptor> _logger;

        public AuthGrpcInterceptor(Func<Task<string>> tokenProvider, ILogger<AuthGrpcInterceptor> logger)
        {
            _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Перехоплює унарні виклики та додає токен авторизації
        /// </summary>
        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
            TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            return InterceptCall(context, continuation, request);
        }

        /// <summary>
        /// Перехоплює серверні потокові виклики
        /// </summary>
        public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
            TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation)
        {
            var metadata = GetMetadataWithToken(context.Options.Headers).GetAwaiter().GetResult();
            var newContext = new ClientInterceptorContext<TRequest, TResponse>(
                context.Method,
                context.Host,
                context.Options.WithHeaders(metadata));

            return continuation(request, newContext);
        }

        /// <summary>
        /// Перехоплює клієнтські потокові виклики
        /// </summary>
        public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation)
        {
            var metadata = GetMetadataWithToken(context.Options.Headers).GetAwaiter().GetResult();
            var newContext = new ClientInterceptorContext<TRequest, TResponse>(
                context.Method,
                context.Host,
                context.Options.WithHeaders(metadata));

            return continuation(newContext);
        }

        /// <summary>
        /// Перехоплює двосторонні потокові виклики
        /// </summary>
        public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation)
        {
            var metadata = GetMetadataWithToken(context.Options.Headers).GetAwaiter().GetResult();
            var newContext = new ClientInterceptorContext<TRequest, TResponse>(
                context.Method,
                context.Host,
                context.Options.WithHeaders(metadata));

            return continuation(newContext);
        }

        /// <summary>
        /// Основний метод для перехоплення унарних викликів
        /// </summary>
        private AsyncUnaryCall<TResponse> InterceptCall<TRequest, TResponse>(
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncUnaryCallContinuation<TRequest, TResponse> continuation,
            TRequest request)
            where TRequest : class
            where TResponse : class
        {
            try
            {
                var metadata = GetMetadataWithToken(context.Options.Headers).GetAwaiter().GetResult();
                var newContext = new ClientInterceptorContext<TRequest, TResponse>(
                    context.Method,
                    context.Host,
                    context.Options.WithHeaders(metadata));

                return continuation(request, newContext);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при додаванні токена авторизації до gRPC виклику");
                throw;
            }
        }

        /// <summary>
        /// Отримує або створює metadata з токеном авторизації
        /// </summary>
        private async Task<Metadata> GetMetadataWithToken(Metadata? existingMetadata)
        {
            var metadata = existingMetadata ?? new Metadata();

            try
            {
                // Отримуємо токен
                var token = await _tokenProvider();

                if (!string.IsNullOrEmpty(token))
                {
                    // Видаляємо старий заголовок авторизації якщо є
                    metadata.Remove(metadata.FirstOrDefault(m =>
                        m.Key.Equals("authorization", StringComparison.OrdinalIgnoreCase)));

                    // Додаємо новий заголовок
                    metadata.Add("Authorization", $"Bearer {token}");

                    _logger.LogDebug("JWT токен успішно додано до gRPC заголовків");
                }
                else
                {
                    _logger.LogWarning("Токен авторизації порожній або не знайдений");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при отриманні токена для gRPC виклику");
                // Продовжуємо без токена, щоб не блокувати виклик
            }

            return metadata;
        }
    }
}
