using CCE.Application.Messages;
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
        RuleFor(c => c.FirstName)
            .NotEmpty().WithErrorCode(MessageKeys.Validation.REQUIRED_FIELD)
            .MaximumLength(50).WithErrorCode(MessageKeys.Validation.MAX_LENGTH)
            .Matches(@"^\p{L}+$").WithErrorCode(MessageKeys.Validation.INVALID_FORMAT);
        RuleFor(c => c.LastName)
            .NotEmpty().WithErrorCode(MessageKeys.Validation.REQUIRED_FIELD)
            .MaximumLength(50).WithErrorCode(MessageKeys.Validation.MAX_LENGTH)
            .Matches(@"^\p{L}+$").WithErrorCode(MessageKeys.Validation.INVALID_FORMAT);
        RuleFor(c => c.Email)
            .NotEmpty().WithErrorCode(MessageKeys.Validation.REQUIRED_FIELD)
            .MaximumLength(100).WithErrorCode(MessageKeys.Validation.MAX_LENGTH)
            .EmailAddress().WithErrorCode(MessageKeys.Validation.INVALID_EMAIL);
        RuleFor(c => c.PhoneNumber)
            .NotEmpty().WithErrorCode(MessageKeys.Validation.REQUIRED_FIELD)
            .MaximumLength(15).WithErrorCode(MessageKeys.Validation.MAX_LENGTH);
        RuleFor(c => c.Role)
            .NotEmpty().WithErrorCode(MessageKeys.Validation.REQUIRED_FIELD)
            .Must(r => AllowedRoles.Contains(r)).WithErrorCode(MessageKeys.Validation.INVALID_ENUM);
    }
}
