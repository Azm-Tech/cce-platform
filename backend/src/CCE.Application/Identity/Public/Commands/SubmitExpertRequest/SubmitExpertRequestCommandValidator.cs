using CCE.Application.Messages;
using FluentValidation;

namespace CCE.Application.Identity.Public.Commands.SubmitExpertRequest;

public sealed class SubmitExpertRequestCommandValidator : AbstractValidator<SubmitExpertRequestCommand>
{
    public SubmitExpertRequestCommandValidator()
    {
        RuleFor(x => x.RequesterId)
            .NotEmpty().WithErrorCode(MessageKeys.Validation.REQUIRED_FIELD);
        RuleFor(x => x.RequestedBioAr)
            .NotEmpty().WithErrorCode(MessageKeys.Validation.REQUIRED_FIELD)
            .MaximumLength(500).WithErrorCode(MessageKeys.Validation.MAX_LENGTH);
        RuleFor(x => x.RequestedBioEn)
            .NotEmpty().WithErrorCode(MessageKeys.Validation.REQUIRED_FIELD)
            .MaximumLength(500).WithErrorCode(MessageKeys.Validation.MAX_LENGTH);
        RuleFor(x => x.RequestedTags)
            .NotNull().WithErrorCode(MessageKeys.Validation.REQUIRED_FIELD)
            .NotEmpty().WithErrorCode(MessageKeys.Validation.REQUIRED_FIELD);
        RuleFor(x => x.CvAssetFileId)
            .NotEmpty().WithErrorCode(MessageKeys.Validation.REQUIRED_FIELD);
    }
}
