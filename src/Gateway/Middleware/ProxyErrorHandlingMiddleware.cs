using System.Text.Json;
using Gateway.Errors;

namespace Gateway.Middleware
{
    public class ProxyErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ProxyErrorHandlingMiddleware> _logger;
        private readonly IHostEnvironment _environment;

        public ProxyErrorHandlingMiddleware(
            RequestDelegate next,
            ILogger<ProxyErrorHandlingMiddleware> logger,
            IHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);

                // Обробка різних статус-кодів помилок
                if (context.Response.StatusCode >= 400)
                {
                    await HandleErrorStatusCodeAsync(context);
                }
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleErrorStatusCodeAsync(HttpContext context)
        {
            var statusCode = context.Response.StatusCode;

            // Визначення типу помилки за статус-кодом
            switch (statusCode)
            {
                case StatusCodes.Status404NotFound:
                    await WriteJsonErrorResponseAsync(context, statusCode, new ApiErrorResponse
                    {
                        Message = "Запитаний ресурс не знайдено",
                        ErrorCode = "RESOURCE_NOT_FOUND"
                    });
                    break;

                case StatusCodes.Status503ServiceUnavailable:
                    string serviceName = GetServiceNameFromPath(context.Request.Path);
                    await WriteJsonErrorResponseAsync(context, statusCode,
                        ApiErrorResponse.ServiceUnavailable(serviceName));
                    break;

                case StatusCodes.Status500InternalServerError:
                case StatusCodes.Status502BadGateway:
                case StatusCodes.Status504GatewayTimeout:
                    await WriteJsonErrorResponseAsync(context, statusCode, new ApiErrorResponse
                    {
                        Message = "Сталася помилка на сервері",
                        ErrorCode = "SERVER_ERROR"
                    });
                    break;

                case StatusCodes.Status401Unauthorized:
                    await WriteJsonErrorResponseAsync(context, statusCode, new ApiErrorResponse
                    {
                        Message = "Необхідна автентифікація",
                        ErrorCode = "UNAUTHORIZED"
                    });
                    break;

                case StatusCodes.Status403Forbidden:
                    await WriteJsonErrorResponseAsync(context, statusCode, new ApiErrorResponse
                    {
                        Message = "Доступ заборонено",
                        ErrorCode = "FORBIDDEN"
                    });
                    break;

                default:
                    // Для інших кодів помилок зберігаємо оригінальну відповідь
                    break;
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            _logger.LogError(exception, "Необроблена помилка в Gateway");

            var response = ApiErrorResponse.FromException(exception);

            if (_environment.IsDevelopment())
            {
                response.Details = new
                {
                    Message = exception.Message,
                    StackTrace = exception.StackTrace,
                    InnerException = exception.InnerException?.Message
                };
            }

            await WriteJsonErrorResponseAsync(context, StatusCodes.Status500InternalServerError, response);
        }

        private async Task WriteJsonErrorResponseAsync(HttpContext context, int statusCode, ApiErrorResponse response)
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }

        private string GetServiceNameFromPath(PathString path)
        {
            if (path.StartsWithSegments("/api/chat") || path.StartsWithSegments("/api/folder"))
                return "ChatService";

            if (path.StartsWithSegments("/api/message") || path.StartsWithSegments("/messageHub"))
                return "MessageService";

            if (path.StartsWithSegments("/api/auth"))
                return "IdentityService";

            if (path.StartsWithSegments("/api/encryption"))
                return "EncryptionService";

            return "API Gateway";
        }
    }
}
