using CCE.Application.Messages;
using FluentValidation;

namespace CCE.Application.Identity.Public.Commands.UpdateMyProfile;

public sealed class UpdateMyProfileCommandValidator : AbstractValidator<UpdateMyProfileCommand>
{
    public UpdateMyProfileCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithErrorCode(MessageKeys.Validation.REQUIRED_FIELD)
            .MaximumLength(100).WithErrorCode(MessageKeys.Validation.MAX_LENGTH);
        RuleFor(x => x.LastName)
            .NotEmpty().WithErrorCode(MessageKeys.Validation.REQUIRED_FIELD)
            .MaximumLength(100).WithErrorCode(MessageKeys.Validation.MAX_LENGTH);
        RuleFor(x => x.JobTitle)
            .NotEmpty().WithErrorCode(MessageKeys.Validation.REQUIRED_FIELD)
            .MaximumLength(200).WithErrorCode(MessageKeys.Validation.MAX_LENGTH);
        RuleFor(x => x.OrganizationName)
            .NotEmpty().WithErrorCode(MessageKeys.Validation.REQUIRED_FIELD)
            .MaximumLength(200).WithErrorCode(MessageKeys.Validation.MAX_LENGTH);

        RuleFor(x => x.LocalePreference)
            .NotEmpty().WithErrorCode(MessageKeys.Validation.REQUIRED_FIELD)
            .Must(l => l == "ar" || l == "en").WithErrorCode(MessageKeys.Validation.INVALID_ENUM);

        RuleFor(x => x.AvatarUrl)
            .Must(url => url is null || url.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase))
            .WithErrorCode(MessageKeys.Validation.INVALID_FORMAT);
    }
}
