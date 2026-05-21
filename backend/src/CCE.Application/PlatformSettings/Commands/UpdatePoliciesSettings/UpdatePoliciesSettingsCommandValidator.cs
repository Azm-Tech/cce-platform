using FluentValidation;

namespace CCE.Application.PlatformSettings.Commands.UpdatePoliciesSettings;

public sealed class UpdatePoliciesSettingsCommandValidator
    : AbstractValidator<UpdatePoliciesSettingsCommand>
{
    public UpdatePoliciesSettingsCommandValidator()
    {
        RuleFor(x => x.RowVersion).NotNull().Must(rv => rv.Length == 8)
            .WithMessage("RowVersion must be exactly 8 bytes.");
    }
}
