using CCE.Application.Messages;
using FluentValidation;

namespace CCE.Application.Identity.Auth.Logout;

public sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
        => RuleFor(x => x.RefreshToken).NotEmpty().WithErrorCode(MessageKeys.Validation.REQUIRED_FIELD);
}
