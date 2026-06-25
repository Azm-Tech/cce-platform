using CCE.Application.Messages;
using CCE.Domain.Community;
using FluentValidation;

namespace CCE.Application.Community.Commands.CreateCommunity;

public sealed class CreateCommunityCommandValidator : AbstractValidator<CreateCommunityCommand>
{
    public CreateCommunityCommandValidator()
    {
        RuleFor(x => x.NameAr).NotEmpty().WithErrorCode(MessageKeys.Validation.REQUIRED_FIELD)
            .MaximumLength(CCE.Domain.Community.Community.MaxNameLength).WithErrorCode(MessageKeys.Validation.MAX_LENGTH);
        RuleFor(x => x.NameEn).NotEmpty().WithErrorCode(MessageKeys.Validation.REQUIRED_FIELD)
            .MaximumLength(CCE.Domain.Community.Community.MaxNameLength).WithErrorCode(MessageKeys.Validation.MAX_LENGTH);
        RuleFor(x => x.Slug).NotEmpty().WithErrorCode(MessageKeys.Validation.REQUIRED_FIELD);
        RuleFor(x => x.Visibility).IsInEnum().WithErrorCode(MessageKeys.Validation.INVALID_ENUM);
    }
}
