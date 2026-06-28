using FluentValidation;

namespace CCE.Application.PlatformSettings.Commands.UpdatePolicySection;

public sealed class UpdatePolicySectionCommandValidator
    : AbstractValidator<UpdatePolicySectionCommand>
{
    public UpdatePolicySectionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.TitleAr).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TitleEn).NotEmpty().MaximumLength(500);
        RuleFor(x => x.ContentAr).NotEmpty();
        RuleFor(x => x.ContentEn).NotEmpty();
    }
}
