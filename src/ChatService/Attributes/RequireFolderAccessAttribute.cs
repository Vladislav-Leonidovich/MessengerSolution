using ChatService.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChatService.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class RequireFolderAccessAttribute : TypeFilterAttribute
    {
        public RequireFolderAccessAttribute()
            : base(typeof(RequireFolderAccessFilter))
        {
        }

        private class RequireFolderAccessFilter : IAsyncActionFilter
        {
            private readonly IChatAuthorizationService _authService;
            private readonly ILogger<RequireFolderAccessFilter> _logger;

            public RequireFolderAccessFilter(
                IChatAuthorizationService authService,
                ILogger<RequireFolderAccessFilter> logger)
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

                // Шукаємо параметр folderId або id
                if (!TryGetFolderId(context, out int folderId))
                {
                    _logger.LogWarning("Не вдалося визначити ID папки з параметрів запиту");
                    context.Result = new BadRequestObjectResult(
                        new { error = "Не вказано ідентифікатор папки" });
                    return;
                }

                // Перевіряємо доступ
                if (!await _authService.CanAccessFolderAsync(userId, folderId))
                {
                    _logger.LogWarning(
                        "Користувачу {UserId} відмовлено в доступі до папки {FolderId}",
                        userId, folderId);

                    context.Result = new ForbidResult();
                    return;
                }

                // Якщо доступ дозволено, продовжуємо виконання
                await next();
            }

            private bool TryGetFolderId(ActionExecutingContext context, out int folderId)
            {
                // Спочатку шукаємо параметр "folderId"
                if (context.ActionArguments.TryGetValue("folderId", out var folderIdObj) &&
                    folderIdObj is int id)
                {
                    folderId = id;
                    return true;
                }

                // Потім пробуємо параметр "id"
                if (context.ActionArguments.TryGetValue("id", out var idObj) &&
                    idObj is int id2)
                {
                    folderId = id2;
                    return true;
                }

                // Пробуємо шукати в моделі запиту
                foreach (var arg in context.ActionArguments.Values)
                {
                    if (arg is null) continue;

                    var property = arg.GetType().GetProperty("FolderId");
                    if (property != null &&
                        property.PropertyType == typeof(int) &&
                        property.GetValue(arg) is int modelFolderId)
                    {
                        folderId = modelFolderId;
                        return true;
                    }
                }

                folderId = 0;
                return false;
            }
        }
    }
}
