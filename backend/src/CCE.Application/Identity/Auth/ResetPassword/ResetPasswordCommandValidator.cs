using CCE.Application.Identity.Auth.Register;
using CCE.Application.Messages;
using FluentValidation;

namespace CCE.Application.Identity.Auth.ResetPassword;

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.EmailAddress)
            .NotEmpty().WithErrorCode(MessageKeys.Validation.REQUIRED_FIELD)
            .EmailAddress().WithErrorCode(MessageKeys.Validation.INVALID_EMAIL)
            .MaximumLength(100).WithErrorCode(MessageKeys.Validation.MAX_LENGTH);

        RuleFor(x => x.Token)
            .NotEmpty().WithErrorCode(MessageKeys.Validation.REQUIRED_FIELD);

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithErrorCode(MessageKeys.Validation.REQUIRED_FIELD)
            .Must(RegisterUserCommandValidator.MatchStrongPasswordPolicy)
            .WithErrorCode(MessageKeys.Validation.PASSWORD_POLICY);

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithErrorCode(MessageKeys.Validation.REQUIRED_FIELD)
            .Equal(x => x.NewPassword)
            .WithErrorCode(MessageKeys.Validation.PASSWORDS_MUST_MATCH);
    }
}
