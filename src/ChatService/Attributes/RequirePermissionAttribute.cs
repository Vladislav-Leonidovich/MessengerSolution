using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Shared.Authorization.Permissions;
using Shared.Authorization;
using System.Security.Claims;

namespace ChatService.Attributes
{
    // Новий атрибут для перевірки дозволів
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public class RequirePermissionAttribute : TypeFilterAttribute
    {
        public RequirePermissionAttribute(ChatPermission permission)
            : base(typeof(RequirePermissionFilter))
        {
            Arguments = new object[] { permission };
        }

        private class RequirePermissionFilter : IAsyncActionFilter
        {
            private readonly ChatPermission _permission;
            private readonly IPermissionService<ChatPermission> _permissionService;
            private readonly ILogger<RequirePermissionFilter> _logger;

            public RequirePermissionFilter(
                ChatPermission permission,
                IPermissionService<ChatPermission> permissionService,
                ILogger<RequirePermissionFilter> logger)
            {
                _permission = permission;
                _permissionService = permissionService;
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

                // Шукаємо ID ресурсу в параметрах запиту
                int? resourceId = TryGetResourceId(context);

                // Перевіряємо дозвіл
                if (!await _permissionService.HasPermissionAsync(userId, _permission, resourceId))
                {
                    _logger.LogWarning(
                        "Користувачу {UserId} відмовлено в доступі через відсутність дозволу {Permission} для ресурсу {ResourceId}",
                        userId, _permission, resourceId);

                    context.Result = new ForbidResult();
                    return;
                }

                // Якщо дозвіл є, продовжуємо виконання
                await next();
            }

            private int? TryGetResourceId(ActionExecutingContext context)
            {
                // Перелік можливих імен параметрів
                var possibleIds = new[] { "id", "chatId", "chatRoomId", "messageId", "folderId" };

                foreach (var paramName in possibleIds)
                {
                    if (context.ActionArguments.TryGetValue(paramName, out var idObj) &&
                        idObj is int id)
                    {
                        return id;
                    }
                }

                // Пробуємо шукати в моделі запиту
                foreach (var arg in context.ActionArguments.Values)
                {
                    if (arg is null) continue;

                    // Шукаємо різні властивості, які можуть бути ID ресурсу
                    foreach (var propName in possibleIds)
                    {
                        var property = arg.GetType().GetProperty(propName);
                        if (property != null &&
                            property.PropertyType == typeof(int) &&
                            property.GetValue(arg) is int propId)
                        {
                            return propId;
                        }
                    }
                }

                return null;
            }
        }
    }
}
