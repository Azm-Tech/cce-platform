using FluentValidation;

namespace CCE.Application.CommunityLaws.Commands.CreateCommunityLawSection;

internal sealed class CreateCommunityLawSectionCommandValidator
    : AbstractValidator<CreateCommunityLawSectionCommand>
{
    public CreateCommunityLawSectionCommandValidator()
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
