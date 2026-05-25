using FluentValidation;

namespace CCE.Application.Identity.Auth.ForgotPassword;

public sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
        => RuleFor(x => x.EmailAddress).NotEmpty().EmailAddress().MaximumLength(100);
}
