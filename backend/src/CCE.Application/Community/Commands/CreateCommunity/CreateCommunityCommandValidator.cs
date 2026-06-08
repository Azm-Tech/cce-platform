using CCE.Application.Errors;
using CCE.Domain.Community;
using FluentValidation;

namespace CCE.Application.Community.Commands.CreateCommunity;

public sealed class CreateCommunityCommandValidator : AbstractValidator<CreateCommunityCommand>
{
    public CreateCommunityCommandValidator()
    {
        RuleFor(x => x.NameAr).NotEmpty().WithErrorCode(ApplicationErrors.Validation.REQUIRED_FIELD)
            .MaximumLength(CCE.Domain.Community.Community.MaxNameLength).WithErrorCode(ApplicationErrors.Validation.MAX_LENGTH);
        RuleFor(x => x.NameEn).NotEmpty().WithErrorCode(ApplicationErrors.Validation.REQUIRED_FIELD)
            .MaximumLength(CCE.Domain.Community.Community.MaxNameLength).WithErrorCode(ApplicationErrors.Validation.MAX_LENGTH);
        RuleFor(x => x.Slug).NotEmpty().WithErrorCode(ApplicationErrors.Validation.REQUIRED_FIELD);
        RuleFor(x => x.Visibility).IsInEnum().WithErrorCode(ApplicationErrors.Validation.INVALID_ENUM);
    }
}
