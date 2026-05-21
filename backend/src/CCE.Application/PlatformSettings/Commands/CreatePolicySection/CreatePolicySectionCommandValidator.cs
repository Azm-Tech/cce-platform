using FluentValidation;

namespace CCE.Application.PlatformSettings.Commands.CreatePolicySection;

public sealed class CreatePolicySectionCommandValidator
    : AbstractValidator<CreatePolicySectionCommand>
{
    public CreatePolicySectionCommandValidator()
    {
        RuleFor(x => x.TitleAr).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TitleEn).NotEmpty().MaximumLength(500);
        RuleFor(x => x.ContentAr).NotEmpty();
        RuleFor(x => x.ContentEn).NotEmpty();
    }
}
