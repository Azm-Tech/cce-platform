using FluentValidation;

namespace CCE.Application.PlatformSettings.Commands.DeleteGlossaryEntry;

public sealed class DeleteGlossaryEntryCommandValidator
    : AbstractValidator<DeleteGlossaryEntryCommand>
{
    public DeleteGlossaryEntryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
