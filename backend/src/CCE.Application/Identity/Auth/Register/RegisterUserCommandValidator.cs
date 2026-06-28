using CCE.Application.Messages;
using FluentValidation;

namespace CCE.Application.Identity.Auth.Register;

public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.FirstName)
            .NotEmpty().WithErrorCode(MessageKeys.Validation.REQUIRED_FIELD)
            .MaximumLength(50).WithErrorCode(MessageKeys.Validation.MAX_LENGTH)
            .Must(BeLettersOnly).WithErrorCode(MessageKeys.Validation.INVALID_FORMAT);

        RuleFor(x => x.LastName)
            .NotEmpty().WithErrorCode(MessageKeys.Validation.REQUIRED_FIELD)
            .MaximumLength(50).WithErrorCode(MessageKeys.Validation.MAX_LENGTH)
            .Must(BeLettersOnly).WithErrorCode(MessageKeys.Validation.INVALID_FORMAT);

        RuleFor(x => x.EmailAddress)
            .NotEmpty().WithErrorCode(MessageKeys.Validation.REQUIRED_FIELD)
            .EmailAddress().WithErrorCode(MessageKeys.Validation.INVALID_EMAIL)
            .MaximumLength(100).WithErrorCode(MessageKeys.Validation.MAX_LENGTH);

        RuleFor(x => x.JobTitle)
            .NotEmpty().WithErrorCode(MessageKeys.Validation.REQUIRED_FIELD)
            .MaximumLength(50).WithErrorCode(MessageKeys.Validation.MAX_LENGTH);

        RuleFor(x => x.OrganizationName)
            .NotEmpty().WithErrorCode(MessageKeys.Validation.REQUIRED_FIELD)
            .MaximumLength(100).WithErrorCode(MessageKeys.Validation.MAX_LENGTH);

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithErrorCode(MessageKeys.Validation.REQUIRED_FIELD)
            .MaximumLength(15).WithErrorCode(MessageKeys.Validation.MAX_LENGTH)
            .Must(BenumbersOnly).WithErrorCode(MessageKeys.Validation.INVALID_PHONE);

        RuleFor(x => x.Password)
            .NotEmpty().WithErrorCode(MessageKeys.Validation.REQUIRED_FIELD)
            .Must(MatchStrongPasswordPolicy).WithErrorCode(MessageKeys.Validation.PASSWORD_POLICY);

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithErrorCode(MessageKeys.Validation.REQUIRED_FIELD)
            .Equal(x => x.Password).WithErrorCode(MessageKeys.Validation.PASSWORDS_MUST_MATCH);
    }

    private static bool BeLettersOnly(string value)
        => !string.IsNullOrWhiteSpace(value) && value.All(char.IsLetter);
    private static bool BenumbersOnly(string value)
        => !string.IsNullOrWhiteSpace(value) && value.All(char.IsNumber);

    internal static bool MatchStrongPasswordPolicy(string value)
        => !string.IsNullOrWhiteSpace(value)
            && value.Length is >= 12 and <= 20
            && value.Any(char.IsUpper)
            && value.Any(char.IsLower)
            && value.Any(char.IsDigit);
}
