using FluentValidation;

namespace CCE.Application.Identity.Public.Commands.ConfirmPhoneChange;

public sealed class ConfirmPhoneChangeCommandValidator : AbstractValidator<ConfirmPhoneChangeCommand>
{
    public ConfirmPhoneChangeCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.VerificationId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().Length(6).Matches(@"^\d{6}$");
    }
}
