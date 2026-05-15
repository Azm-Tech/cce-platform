using CCE.Application.Identity.Auth.Register;
using FluentValidation;

namespace CCE.Application.Identity.Auth.ResetPassword;

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.EmailAddress).NotEmpty().EmailAddress().MaximumLength(100);
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword).Must(RegisterUserCommandValidator.MatchStoryPasswordPolicy).WithMessage("PASSWORD_POLICY");
        RuleFor(x => x.ConfirmPassword).Equal(x => x.NewPassword);
    }
}
