using CCE.Application.Messages;
using FluentValidation;

namespace CCE.Application.Community.Commands.VotePost;

public sealed class VotePostCommandValidator : AbstractValidator<VotePostCommand>
{
    public VotePostCommandValidator()
    {
        RuleFor(x => x.PostId).NotEmpty().WithErrorCode(MessageKeys.Validation.REQUIRED_FIELD);
        RuleFor(x => x.Direction).IsInEnum().WithErrorCode(MessageKeys.Validation.INVALID_ENUM);
    }
}
