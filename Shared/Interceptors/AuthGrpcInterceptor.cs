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
        private readonly ILogger _logger;

        public AuthGrpcInterceptor(Func<Task<string>> tokenProvider, ILogger logger)
        {
            _tokenProvider = tokenProvider;
            _logger = logger;
        }

        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
            TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            return AddTokenToCall(request, context, continuation);
        }

        public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation)
        {
            return AddTokenToCall(context, continuation);
        }

        public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
            TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation)
        {
            return AddTokenToCall(request, context, continuation);
        }

        private AsyncUnaryCall<TResponse> AddTokenToCall<TRequest, TResponse>(
            TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
            where TRequest : class
            where TResponse : class
        {
            try
            {
                var headers = GetAuthHeadersAsync().GetAwaiter().GetResult();
                if (headers != null)
                {
                    var callOptions = context.Options.WithHeaders(headers);
                    context = new ClientInterceptorContext<TRequest, TResponse>(
                        context.Method, context.Host, callOptions);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка додавання токена аутентифікації в gRPC-виклик");
            }

            return continuation(request, context);
        }

        private AsyncClientStreamingCall<TRequest, TResponse> AddTokenToCall<TRequest, TResponse>(
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation)
            where TRequest : class
            where TResponse : class
        {
            try
            {
                var headers = GetAuthHeadersAsync().GetAwaiter().GetResult();
                if (headers != null)
                {
                    var callOptions = context.Options.WithHeaders(headers);
                    context = new ClientInterceptorContext<TRequest, TResponse>(
                        context.Method, context.Host, callOptions);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка додавання токена аутентифікації в gRPC-виклик");
            }

            return continuation(context);
        }

        private AsyncServerStreamingCall<TResponse> AddTokenToCall<TRequest, TResponse>(
            TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation)
            where TRequest : class
            where TResponse : class
        {
            try
            {
                var headers = GetAuthHeadersAsync().GetAwaiter().GetResult();
                if (headers != null)
                {
                    var callOptions = context.Options.WithHeaders(headers);
                    context = new ClientInterceptorContext<TRequest, TResponse>(
                        context.Method, context.Host, callOptions);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка додавання токена аутентифікації в gRPC-виклик");
            }

            return continuation(request, context);
        }

        private async Task<Metadata?> GetAuthHeadersAsync()
        {
            var token = await _tokenProvider();
            if (string.IsNullOrEmpty(token))
            {
                return null;
            }

            var headers = new Metadata
        {
            { "Authorization", $"Bearer {token}" }
        };

            return headers;
        }
    }
}
