using FluentValidation;

namespace CCE.Application.PlatformSettings.Commands.DeletePolicySection;

public sealed class DeletePolicySectionCommandValidator
    : AbstractValidator<DeletePolicySectionCommand>
{
    public DeletePolicySectionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
