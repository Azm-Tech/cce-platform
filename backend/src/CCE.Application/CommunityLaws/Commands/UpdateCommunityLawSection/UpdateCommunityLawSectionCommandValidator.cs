using FluentValidation;

namespace CCE.Application.CommunityLaws.Commands.UpdateCommunityLawSection;

internal sealed class UpdateCommunityLawSectionCommandValidator
    : AbstractValidator<UpdateCommunityLawSectionCommand>
{
    public UpdateCommunityLawSectionCommandValidator()
    {
        RuleFor(x => x.TitleAr)
            .NotEmpty().WithErrorCode("REQUIRED_FIELD")
            .MaximumLength(500).WithErrorCode("MAX_LENGTH");

        RuleFor(x => x.TitleEn)
            .NotEmpty().WithErrorCode("REQUIRED_FIELD")
            .MaximumLength(500).WithErrorCode("MAX_LENGTH");

        RuleFor(x => x.ContentAr)
            .NotEmpty().WithErrorCode("REQUIRED_FIELD")
            .MaximumLength(10000).WithErrorCode("MAX_LENGTH");

        RuleFor(x => x.ContentEn)
            .NotEmpty().WithErrorCode("REQUIRED_FIELD")
            .MaximumLength(10000).WithErrorCode("MAX_LENGTH");
    }
}
