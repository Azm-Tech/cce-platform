using FluentValidation;

namespace CCE.Application.PlatformSettings.Commands.DeleteFaq;

public sealed class DeleteFaqCommandValidator
    : AbstractValidator<DeleteFaqCommand>
{
    public DeleteFaqCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
