using CCE.Application.Messages;
using FluentValidation;

namespace CCE.Application.Community.Commands.VoteReply;

public sealed class VoteReplyCommandValidator : AbstractValidator<VoteReplyCommand>
{
    public VoteReplyCommandValidator()
    {
        RuleFor(x => x.ReplyId).NotEmpty().WithErrorCode(MessageKeys.Validation.REQUIRED_FIELD);
        RuleFor(x => x.Direction).IsInEnum().WithErrorCode(MessageKeys.Validation.INVALID_ENUM);
    }
}
