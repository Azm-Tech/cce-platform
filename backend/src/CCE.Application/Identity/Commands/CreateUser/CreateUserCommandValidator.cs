using FluentValidation;

namespace CCE.Application.Identity.Commands.CreateUser;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    private static readonly HashSet<string> AllowedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "cce-admin",
        "cce-content-manager",
        "cce-state-representative",
    };

    public CreateUserCommandValidator()
    {
        RuleFor(c => c.FirstName).NotEmpty().MaximumLength(50)
            .Matches(@"^\p{L}+$").WithMessage("First name must contain letters only.");
        RuleFor(c => c.LastName).NotEmpty().MaximumLength(50)
            .Matches(@"^\p{L}+$").WithMessage("Last name must contain letters only.");
        RuleFor(c => c.Email).NotEmpty().MaximumLength(100).EmailAddress();
        RuleFor(c => c.PhoneNumber).NotEmpty().MaximumLength(15);
        RuleFor(c => c.Role).NotEmpty().Must(r => AllowedRoles.Contains(r))
            .WithMessage($"Role must be one of: {string.Join(", ", AllowedRoles)}.");
    }
}
