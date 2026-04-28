using FluentValidation;

namespace CCE.Application.Content.Commands.RejectCountryResourceRequest;

public sealed class RejectCountryResourceRequestCommandValidator
    : AbstractValidator<RejectCountryResourceRequestCommand>
{
    public RejectCountryResourceRequestCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.AdminNotesAr).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.AdminNotesEn).NotEmpty().MaximumLength(2000);
    }
}
