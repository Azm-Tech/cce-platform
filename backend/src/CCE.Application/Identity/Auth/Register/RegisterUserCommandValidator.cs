using FluentValidation;

namespace CCE.Application.Identity.Auth.Register;

public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(50).Must(BeLettersOnly);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(50).Must(BeLettersOnly);
        RuleFor(x => x.EmailAddress).NotEmpty().EmailAddress().MaximumLength(100);
        RuleFor(x => x.JobTitle).NotEmpty().MaximumLength(50);
        RuleFor(x => x.OrganizationName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(15);
        RuleFor(x => x.Password).Must(MatchStoryPasswordPolicy).WithMessage("PASSWORD_POLICY");
        RuleFor(x => x.ConfirmPassword).Equal(x => x.Password);
    }

    private static bool BeLettersOnly(string value)
        => !string.IsNullOrWhiteSpace(value) && value.All(char.IsLetter);

    internal static bool MatchStoryPasswordPolicy(string value)
        => !string.IsNullOrWhiteSpace(value)
            && value.Length is >= 12 and <= 20
            && value.Any(char.IsUpper)
            && value.Any(char.IsLower)
            && value.Any(char.IsDigit);
}
