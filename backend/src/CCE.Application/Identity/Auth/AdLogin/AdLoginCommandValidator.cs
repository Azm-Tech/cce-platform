using FluentValidation;

namespace CCE.Application.Identity.Auth.AdLogin;

public sealed class AdLoginCommandValidator : AbstractValidator<AdLoginCommand>
{
    public AdLoginCommandValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .WithMessage("Username is required.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required.");
    }
}
