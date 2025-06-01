using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs.Responses;

namespace MessageService.Attributes
{
    public class ValidateModelStateAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                var errors = context.ModelState
                    .Where(e => e.Value.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                var result = new ApiResponse<object>
                {
                    Success = false,
                    Message = "Помилка валідації вхідних даних",
                    Errors = errors.SelectMany(e => e.Value).ToList()
                };

                context.Result = new BadRequestObjectResult(result);
            }
        }
    }
}
