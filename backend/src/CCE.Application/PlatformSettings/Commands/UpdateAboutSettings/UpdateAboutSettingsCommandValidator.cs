using FluentValidation;

namespace CCE.Application.PlatformSettings.Commands.UpdateAboutSettings;

public sealed class UpdateAboutSettingsCommandValidator
    : AbstractValidator<UpdateAboutSettingsCommand>
{
    public UpdateAboutSettingsCommandValidator()
    {
        RuleFor(x => x.DescriptionAr).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.DescriptionEn).NotEmpty().MaximumLength(1000);
    }
}
