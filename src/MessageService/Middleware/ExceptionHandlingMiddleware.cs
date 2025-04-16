using Shared.Exceptions;
using System.Net;
using System.Text.Json;

namespace MessageService.Middleware
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
            HttpStatusCode statusCode = HttpStatusCode.InternalServerError;
            string errorMessage = "Сталася внутрішня помилка сервера.";
            var errorDetails = new Dictionary<string, string[]>();

            // Определяем тип исключения и соответствующий статус-код
            switch (exception)
            {
                case EntityNotFoundException ex:
                    statusCode = HttpStatusCode.NotFound;
                    errorMessage = ex.Message;
                    _logger.LogInformation(ex, "Запит до неіснуючого ресурсу: {EntityName} {EntityId}",
                        ex.EntityName, ex.EntityId);
                    break;

                case ForbiddenAccessException ex:
                    statusCode = HttpStatusCode.Forbidden;
                    errorMessage = ex.Message;
                    _logger.LogWarning(ex, "Спроба доступу до забороненого ресурсу");
                    break;

                case ValidationException ex:
                    statusCode = HttpStatusCode.BadRequest;
                    errorMessage = ex.Message;
                    errorDetails = (Dictionary<string, string[]>)ex.Errors;
                    _logger.LogWarning(ex, "Помилка валідації даних");
                    break;

                case DatabaseException ex:
                    _logger.LogError(ex, "Помилка бази даних");
                    break;

                default:
                    _logger.LogError(exception, "Необроблена помилка");
                    break;
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var response = new
            {
                success = false,
                message = errorMessage,
                errors = errorDetails.Any() ? errorDetails : null,
                detail = _environment.IsDevelopment() ? exception.ToString() : null
            };

            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(jsonResponse);
        }
    }
}
