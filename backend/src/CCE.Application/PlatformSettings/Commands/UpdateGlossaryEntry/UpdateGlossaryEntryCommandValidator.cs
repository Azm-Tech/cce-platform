using FluentValidation;

namespace CCE.Application.PlatformSettings.Commands.UpdateGlossaryEntry;

public sealed class UpdateGlossaryEntryCommandValidator
    : AbstractValidator<UpdateGlossaryEntryCommand>
{
    public UpdateGlossaryEntryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.TermAr).NotEmpty().MaximumLength(100);
        RuleFor(x => x.TermEn).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DefinitionAr).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.DefinitionEn).NotEmpty().MaximumLength(1000);
    }
}
