using FluentValidation;

namespace CCE.Application.Content.Commands.ApproveCountryResourceRequest;

public sealed class ApproveCountryResourceRequestCommandValidator
    : AbstractValidator<ApproveCountryResourceRequestCommand>
{
    public ApproveCountryResourceRequestCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.AdminNotesAr).MaximumLength(2000);
        RuleFor(x => x.AdminNotesEn).MaximumLength(2000);
    }
}
