using CCE.Application.Messages;
using FluentValidation;

namespace CCE.Application.Identity.Auth.RefreshToken;

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
        => RuleFor(x => x.RefreshToken).NotEmpty().WithErrorCode(MessageKeys.Validation.REQUIRED_FIELD);
}
