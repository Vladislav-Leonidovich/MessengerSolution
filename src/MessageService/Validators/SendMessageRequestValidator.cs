using FluentValidation;
using Shared.DTOs.Requests;

namespace MessageService.Validators
{
    public class SendMessageRequestValidator : AbstractValidator<SendMessageRequest>
    {
        public SendMessageRequestValidator()
        {
            RuleFor(x => x).Must(x => !string.IsNullOrWhiteSpace(x.Content) && (x.Content.Length < 1000 ))
                .WithMessage("Повідомлення повинно містити текст");
        }
    }
}
