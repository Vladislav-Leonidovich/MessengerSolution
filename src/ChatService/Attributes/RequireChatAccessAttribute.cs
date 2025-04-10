using ChatService.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChatService.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class RequireChatAccessAttribute : TypeFilterAttribute
    {
        public RequireChatAccessAttribute()
            : base(typeof(RequireChatAccessFilter))
        {
        }

        private class RequireChatAccessFilter : IAsyncActionFilter
        {
            private readonly IChatAuthorizationService _authService;
            private readonly ILogger<RequireChatAccessFilter> _logger;

            public RequireChatAccessFilter(
                IChatAuthorizationService authService,
                ILogger<RequireChatAccessFilter> logger)
            {
                _authService = authService;
                _logger = logger;
            }

            public async Task OnActionExecutionAsync(
                ActionExecutingContext context,
                ActionExecutionDelegate next)
            {
                // Отримуємо ID користувача з токена
                var userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    _logger.LogWarning("Спроба доступу без авторизації");
                    context.Result = new UnauthorizedResult();
                    return;
                }

                // Шукаємо параметр chatRoomId або id
                if (!TryGetChatRoomId(context, out int chatRoomId))
                {
                    _logger.LogWarning("Не вдалося визначити ID чату з параметрів запиту");
                    context.Result = new BadRequestObjectResult(
                        new { error = "Не вказано ідентифікатор чату" });
                    return;
                }

                // Перевіряємо доступ
                if (!await _authService.CanAccessChatRoom(userId, chatRoomId))
                {
                    _logger.LogWarning(
                        "Користувачу {UserId} відмовлено в доступі до чату {ChatRoomId}",
                        userId, chatRoomId);

                    context.Result = new ForbidResult();
                    return;
                }

                // Якщо доступ дозволено, продовжуємо виконання
                await next();
            }

            private bool TryGetChatRoomId(ActionExecutingContext context, out int chatRoomId)
            {
                // Спочатку шукаємо параметр "chatRoomId"
                if (context.ActionArguments.TryGetValue("chatRoomId", out var chatIdObj) &&
                    chatIdObj is int chatId)
                {
                    chatRoomId = chatId;
                    return true;
                }

                // Потім пробуємо параметр "id"
                if (context.ActionArguments.TryGetValue("id", out var idObj) &&
                    idObj is int id)
                {
                    chatRoomId = id;
                    return true;
                }

                // Пробуємо шукати в моделі запиту
                foreach (var arg in context.ActionArguments.Values)
                {
                    if (arg is null) continue;

                    var property = arg.GetType().GetProperty("ChatRoomId");
                    if (property != null &&
                        property.PropertyType == typeof(int) &&
                        property.GetValue(arg) is int modelChatId)
                    {
                        chatRoomId = modelChatId;
                        return true;
                    }
                }

                chatRoomId = 0;
                return false;
            }
        }
    }
}
