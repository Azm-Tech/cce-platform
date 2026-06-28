using FluentValidation;

namespace CCE.Application.PlatformSettings.Commands.UpdateHomepageSettings;

public sealed class UpdateHomepageSettingsCommandValidator
    : AbstractValidator<UpdateHomepageSettingsCommand>
{
    public UpdateHomepageSettingsCommandValidator()
    {
        RuleFor(x => x.ObjectiveAr).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.ObjectiveEn).NotEmpty().MaximumLength(1000);
    }
}
