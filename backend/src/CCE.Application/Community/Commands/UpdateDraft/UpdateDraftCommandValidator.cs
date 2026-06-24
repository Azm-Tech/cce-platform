using CCE.Application.Messages;
using CCE.Domain.Community;
using FluentValidation;

namespace CCE.Application.Community.Commands.UpdateDraft;

public sealed class UpdateDraftCommandValidator : AbstractValidator<UpdateDraftCommand>
{
    public UpdateDraftCommandValidator()
    {
        RuleFor(x => x.PostId).NotEmpty().WithErrorCode(MessageKeys.Validation.REQUIRED_FIELD);
        RuleFor(x => x.Title)
            .NotEmpty().WithErrorCode(MessageKeys.Validation.REQUIRED_FIELD)
            .MaximumLength(Post.MaxTitleLength).WithErrorCode(MessageKeys.Validation.MAX_LENGTH);
        RuleFor(x => x.Content)
            .MaximumLength(Post.MaxContentLength).WithErrorCode(MessageKeys.Validation.MAX_LENGTH);
    }
}
