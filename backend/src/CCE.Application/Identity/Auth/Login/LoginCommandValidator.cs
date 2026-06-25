using CCE.Application.Messages;
using FluentValidation;

namespace CCE.Application.Identity.Auth.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.EmailAddress)
            .NotEmpty().WithErrorCode(MessageKeys.Validation.REQUIRED_FIELD)
            .EmailAddress().WithErrorCode(MessageKeys.Validation.INVALID_EMAIL)
            .MaximumLength(100).WithErrorCode(MessageKeys.Validation.MAX_LENGTH);
        RuleFor(x => x.Password)
            .NotEmpty().WithErrorCode(MessageKeys.Validation.REQUIRED_FIELD);
    }
}
