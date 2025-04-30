using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using MessageService.Authorization;

namespace MessageService.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class RequireMessageAccessAttribute : TypeFilterAttribute
    {
        public RequireMessageAccessAttribute()
            : base(typeof(RequireMessageAccessFilter))
        {
        }

        private class RequireMessageAccessFilter : IAsyncActionFilter
        {
            private readonly IMessageAuthorizationService _authService;
            private readonly ILogger<RequireMessageAccessFilter> _logger;

            public RequireMessageAccessFilter(
                IMessageAuthorizationService authService,
                ILogger<RequireMessageAccessFilter> logger)
            {
                _authService = authService;
                _logger = logger;
            }

            public async Task OnActionExecutionAsync(
                ActionExecutingContext context,
                ActionExecutionDelegate next)
            {
                // Получаем ID пользователя из токена
                var userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    _logger.LogWarning("Спроба доступу без авторизації");
                    context.Result = new UnauthorizedResult();
                    return;
                }

                // Ищем параметр messageId или chatRoomId
                if (!TryGetResourceId(context, out int resourceId, out ResourceType resourceType))
                {
                    _logger.LogWarning("Не вдалося визначити ID ресурсу з параметрів запиту");
                    context.Result = new BadRequestObjectResult(
                        new { error = "Не вказано ідентифікатор ресурсу" });
                    return;
                }

                bool hasAccess;

                // Проверяем доступ в зависимости от типа ресурса
                switch (resourceType)
                {
                    case ResourceType.Message:
                        _logger.LogInformation("Перевірка доступу до повідомлення {MessageId} для користувача {UserId}", resourceId, userId);
                        hasAccess = await _authService.CanAccessMessageAsync(userId, resourceId);
                        break;
                    case ResourceType.Chat:
                        _logger.LogInformation("Перевірка доступу до чату {ChatRoomId} для користувача {UserId}", resourceId, userId);
                        hasAccess = await _authService.CanAccessChatRoomAsync(userId, resourceId);
                        break;
                    default:
                        _logger.LogWarning("Невідомий тип ресурсу: {ResourceType}", resourceType);
                        context.Result = new BadRequestObjectResult(
                            new { error = "Невідомий тип ресурсу" });

                        return;
                }

                if (!hasAccess)
                {
                    _logger.LogWarning(
                        "Користувачеві {UserId} відмовлено в доступі до ресурсу {ResourceType} {ResourceId}",
                        userId, resourceType, resourceId);

                    context.Result = new ForbidResult();
                    return;
                }

                // Если доступ разрешен, продолжаем выполнение
                await next();
            }

            private bool TryGetResourceId(ActionExecutingContext context, out int resourceId, out ResourceType resourceType)
            {
                // Проверяем наличие messageId
                if (context.ActionArguments.TryGetValue("messageId", out var messageIdObj) &&
                    messageIdObj is int messageId)
                {
                    resourceId = messageId;
                    resourceType = ResourceType.Message;
                    return true;
                }

                // Проверяем наличие chatRoomId
                if (context.ActionArguments.TryGetValue("chatRoomId", out var chatRoomIdObj) &&
                    chatRoomIdObj is int chatRoomId)
                {
                    resourceId = chatRoomId;
                    resourceType = ResourceType.Chat;
                    return true;
                }

                // Проверяем в модели запроса
                foreach (var arg in context.ActionArguments.Values)
                {
                    if (arg == null) continue;

                    var messageIdProperty = arg.GetType().GetProperty("MessageId");
                    if (messageIdProperty != null &&
                        messageIdProperty.PropertyType == typeof(int) &&
                        messageIdProperty.GetValue(arg) is int msgId)
                    {
                        resourceId = msgId;
                        resourceType = ResourceType.Message;
                        return true;
                    }

                    var chatRoomIdProperty = arg.GetType().GetProperty("ChatRoomId");
                    if (chatRoomIdProperty != null &&
                        chatRoomIdProperty.PropertyType == typeof(int) &&
                        chatRoomIdProperty.GetValue(arg) is int chatId)
                    {
                        resourceId = chatId;
                        resourceType = ResourceType.Chat;
                        return true;
                    }
                }

                resourceId = 0;
                resourceType = ResourceType.None;
                return false;
            }
        }
    }
}
