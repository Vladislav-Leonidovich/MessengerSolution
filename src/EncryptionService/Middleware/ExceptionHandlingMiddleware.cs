using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace EncryptionService.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
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
            HttpStatusCode statusCode = HttpStatusCode.InternalServerError; // 500 по умолчанию
            string errorMessage = "Сталася внутрішня помилка сервера.";

            // Определяем тип исключения и соответствующий статус-код
            switch (exception)
            {
                case UnauthorizedAccessException:
                    statusCode = HttpStatusCode.Forbidden;
                    errorMessage = "У вас немає доступу до цього ресурсу.";
                    _logger.LogWarning(exception, "Спроба доступу до забороненого ресурсу");
                    break;

                case KeyNotFoundException:
                    statusCode = HttpStatusCode.NotFound;
                    errorMessage = "Запитуваний ресурс не знайдено.";
                    _logger.LogInformation(exception, "Запит до неіснуючого ресурсу");
                    break;

                case ArgumentException:
                case FormatException:
                    statusCode = HttpStatusCode.BadRequest;
                    errorMessage = "Неправильний формат запиту.";
                    _logger.LogWarning(exception, "Неправильний формат запиту");
                    break;

                default:
                    _logger.LogError(exception, "Необроблена помилка");
                    break;
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var response = new
            {
                error = new
                {
                    message = errorMessage,
                    detail = exception.Message,
                    // В Production не возвращаем стек-трейс
                    stackTrace = context.Request.Host.Host.Contains("localhost") ? exception.StackTrace : null
                }
            };

            var jsonResponse = JsonSerializer.Serialize(response);
            await context.Response.WriteAsync(jsonResponse);
        }
    }
}
