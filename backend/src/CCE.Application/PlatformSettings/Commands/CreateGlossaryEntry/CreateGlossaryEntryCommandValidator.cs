using FluentValidation;

namespace CCE.Application.PlatformSettings.Commands.CreateGlossaryEntry;

public sealed class CreateGlossaryEntryCommandValidator
    : AbstractValidator<CreateGlossaryEntryCommand>
{
    public CreateGlossaryEntryCommandValidator()
    {
        RuleFor(x => x.TermAr).NotEmpty().MaximumLength(100);
        RuleFor(x => x.TermEn).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DefinitionAr).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.DefinitionEn).NotEmpty().MaximumLength(1000);
    }
}
