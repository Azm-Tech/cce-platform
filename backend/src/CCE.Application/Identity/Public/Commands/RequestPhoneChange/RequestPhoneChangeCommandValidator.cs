using FluentValidation;

namespace CCE.Application.Identity.Public.Commands.RequestPhoneChange;

public sealed class RequestPhoneChangeCommandValidator : AbstractValidator<RequestPhoneChangeCommand>
{
    public RequestPhoneChangeCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.NewPhone).NotEmpty().Matches(@"^\d{7,15}$");
    }
}
