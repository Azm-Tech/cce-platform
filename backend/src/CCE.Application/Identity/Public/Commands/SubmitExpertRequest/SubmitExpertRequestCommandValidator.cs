using FluentValidation;

namespace CCE.Application.Identity.Public.Commands.SubmitExpertRequest;

public sealed class SubmitExpertRequestCommandValidator : AbstractValidator<SubmitExpertRequestCommand>
{
    public SubmitExpertRequestCommandValidator()
    {
        RuleFor(x => x.RequesterId).NotEmpty();
        RuleFor(x => x.RequestedBioAr).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.RequestedBioEn).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.RequestedTags).NotNull();
    }
}
