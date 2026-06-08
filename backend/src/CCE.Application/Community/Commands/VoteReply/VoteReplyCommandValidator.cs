using CCE.Application.Errors;
using FluentValidation;

namespace CCE.Application.Community.Commands.VoteReply;

public sealed class VoteReplyCommandValidator : AbstractValidator<VoteReplyCommand>
{
    public VoteReplyCommandValidator()
    {
        RuleFor(x => x.ReplyId).NotEmpty().WithErrorCode(ApplicationErrors.Validation.REQUIRED_FIELD);
        RuleFor(x => x.Direction).IsInEnum().WithErrorCode(ApplicationErrors.Validation.INVALID_ENUM);
    }
}
