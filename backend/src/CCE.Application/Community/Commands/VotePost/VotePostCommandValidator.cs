using CCE.Application.Errors;
using FluentValidation;

namespace CCE.Application.Community.Commands.VotePost;

public sealed class VotePostCommandValidator : AbstractValidator<VotePostCommand>
{
    public VotePostCommandValidator()
    {
        RuleFor(x => x.PostId).NotEmpty().WithErrorCode(ApplicationErrors.Validation.REQUIRED_FIELD);
        RuleFor(x => x.Direction).IsInEnum().WithErrorCode(ApplicationErrors.Validation.INVALID_ENUM);
    }
}
