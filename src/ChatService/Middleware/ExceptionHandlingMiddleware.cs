using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Grpc.Core;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shared.Exceptions;

namespace ChatService.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IWebHostEnvironment _environment;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger,
            IWebHostEnvironment environment)
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
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var (statusCode, errorResponse) = MapExceptionToResponse(exception);

            // Логування помилок
            LogException(exception, context);

            // Налаштування відповіді
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            // Серіалізація та відправка відповіді
            var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = _environment.IsDevelopment()
            });

            await context.Response.WriteAsync(jsonResponse);
        }

        // Мапить виняток на HTTP відповідь
        private (HttpStatusCode statusCode, object response) MapExceptionToResponse(Exception exception)
        {
            return exception switch
            {
                // Бізнес-логічні винятки (4xx)
                EntityNotFoundException ex => (
                    HttpStatusCode.NotFound,
                    CreateErrorResponse("ENTITY_NOT_FOUND", ex.Message)
                ),

                ForbiddenAccessException ex => (
                    HttpStatusCode.Forbidden,
                    CreateErrorResponse("ACCESS_FORBIDDEN", ex.Message)
                ),

                ValidationException ex => (
                    HttpStatusCode.BadRequest,
                    CreateValidationErrorResponse(ex)
                ),

                ArgumentException ex => (
                    HttpStatusCode.BadRequest,
                    CreateErrorResponse("INVALID_ARGUMENT", ex.Message)
                ),

                UnauthorizedAccessException ex => (
                    HttpStatusCode.Unauthorized,
                    CreateErrorResponse("UNAUTHORIZED", "Необхідна автентифікація")
                ),

                InvalidOperationException ex => (
                    HttpStatusCode.BadRequest,
                    CreateErrorResponse("INVALID_OPERATION", ex.Message)
                ),

                TimeoutException ex => (
                    HttpStatusCode.RequestTimeout,
                    CreateErrorResponse("TIMEOUT", "Операція перевищила ліміт часу")
                ),

                // MassTransit винятки
                RequestTimeoutException ex => (
                    HttpStatusCode.RequestTimeout,
                    CreateErrorResponse("MESSAGE_TIMEOUT", "Час очікування відповіді від сервісу минув")
                ),

                RequestException ex => (
                    HttpStatusCode.BadGateway,
                    CreateErrorResponse("SERVICE_ERROR", "Помилка при взаємодії з сервісом")
                ),

                // gRPC винятки
                RpcException ex when ex.StatusCode == StatusCode.NotFound => (
                    HttpStatusCode.NotFound,
                    CreateErrorResponse("GRPC_NOT_FOUND", "Ресурс не знайдено")
                ),

                RpcException ex when ex.StatusCode == StatusCode.Unauthenticated => (
                    HttpStatusCode.Unauthorized,
                    CreateErrorResponse("GRPC_UNAUTHENTICATED", "Помилка автентифікації")
                ),

                RpcException ex when ex.StatusCode == StatusCode.PermissionDenied => (
                    HttpStatusCode.Forbidden,
                    CreateErrorResponse("GRPC_PERMISSION_DENIED", "Доступ заборонено")
                ),

                RpcException ex => (
                    HttpStatusCode.BadGateway,
                    CreateErrorResponse("GRPC_ERROR", "Помилка gRPC сервісу")
                ),

                // Системні винятки (5xx)
                DatabaseException ex => (
                    HttpStatusCode.InternalServerError,
                    CreateErrorResponse("DATABASE_ERROR", "Помилка при роботі з базою даних")
                ),

                ServiceUnavailableException ex => (
                    HttpStatusCode.ServiceUnavailable,
                    CreateErrorResponse("SERVICE_UNAVAILABLE", ex.Message)
                ),

                // Загальні винятки
                _ => (
                    HttpStatusCode.InternalServerError,
                    CreateErrorResponse("INTERNAL_ERROR", "Сталася внутрішня помилка сервера")
                )
            };
        }

        // Створює стандартну відповідь про помилку
        private object CreateErrorResponse(string errorCode, string message, object? details = null)
        {
            var response = new
            {
                success = false,
                errorCode,
                message,
                details,
                timestamp = DateTime.UtcNow
            };

            // В development режимі додаємо технічну інформацію
            if (_environment.IsDevelopment() && details == null)
            {
                return new
                {
                    response.success,
                    response.errorCode,
                    response.message,
                    response.timestamp,
                    technicalDetails = new
                    {
                        environment = _environment.EnvironmentName,
                        machineName = Environment.MachineName
                    }
                };
            }

            return response;
        }

        // Створює відповідь для помилок валідації
        private object CreateValidationErrorResponse(ValidationException ex)
        {
            return new
            {
                success = false,
                errorCode = "VALIDATION_ERROR",
                message = ex.Message,
                errors = ex.Errors,
                timestamp = DateTime.UtcNow
            };
        }

        // Логує виняток з відповідним рівнем
        private void LogException(Exception exception, HttpContext context)
        {
            var requestPath = context.Request.Path;
            var requestMethod = context.Request.Method;
            var userAgent = context.Request.Headers.UserAgent.ToString();

            // Контекстна інформація
            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["RequestPath"] = requestPath,
                ["RequestMethod"] = requestMethod,
                ["UserAgent"] = userAgent,
                ["TraceId"] = context.TraceIdentifier
            });

            switch (exception)
            {
                // Не логуємо як помилки - це очікувані винятки
                case EntityNotFoundException:
                case ForbiddenAccessException:
                case ValidationException:
                case ArgumentException:
                case UnauthorizedAccessException:
                    _logger.LogWarning(exception, "Клієнтська помилка: {ExceptionType} - {Message}",
                        exception.GetType().Name, exception.Message);
                    break;

                // Логуємо як помилки системи
                case DatabaseException:
                case ServiceUnavailableException:
                    _logger.LogError(exception, "Системна помилка: {ExceptionType} - {Message}",
                        exception.GetType().Name, exception.Message);
                    break;

                // MassTransit та gRPC помилки
                case RequestTimeoutException:
                case RequestException:
                case RpcException:
                    _logger.LogWarning(exception, "Помилка зовнішнього сервісу: {ExceptionType} - {Message}",
                        exception.GetType().Name, exception.Message);
                    break;

                // Критичні помилки
                default:
                    _logger.LogError(exception, "Необроблена помилка: {ExceptionType} - {Message}",
                        exception.GetType().Name, exception.Message);
                    break;
            }
        }
    }
}
