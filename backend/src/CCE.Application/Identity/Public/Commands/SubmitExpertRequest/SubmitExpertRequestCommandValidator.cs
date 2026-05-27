using FluentValidation;

namespace CCE.Application.Identity.Public.Commands.SubmitExpertRequest;

public sealed class SubmitExpertRequestCommandValidator : AbstractValidator<SubmitExpertRequestCommand>
{
    public SubmitExpertRequestCommandValidator()
    {
        RuleFor(x => x.RequesterId).NotEmpty();
        RuleFor(x => x.RequestedBioAr).NotEmpty().MaximumLength(500);
        RuleFor(x => x.RequestedBioEn).NotEmpty().MaximumLength(500);
        RuleFor(x => x.RequestedTags).NotNull().NotEmpty()
            .WithMessage("At least one expertise tag is required.");
        RuleFor(x => x.CvAssetFileId).NotEmpty();
    }
}
