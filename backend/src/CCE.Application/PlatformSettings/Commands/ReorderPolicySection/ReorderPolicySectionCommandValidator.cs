using FluentValidation;

namespace CCE.Application.PlatformSettings.Commands.ReorderPolicySection;

public sealed class ReorderPolicySectionCommandValidator
    : AbstractValidator<ReorderPolicySectionCommand>
{
    public ReorderPolicySectionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.OrderIndex).GreaterThanOrEqualTo(0);
    }
}
