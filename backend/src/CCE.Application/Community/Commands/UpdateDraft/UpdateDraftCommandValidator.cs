using CCE.Application.Errors;
using CCE.Domain.Community;
using FluentValidation;

namespace CCE.Application.Community.Commands.UpdateDraft;

public sealed class UpdateDraftCommandValidator : AbstractValidator<UpdateDraftCommand>
{
    public UpdateDraftCommandValidator()
    {
        RuleFor(x => x.PostId).NotEmpty().WithErrorCode(ApplicationErrors.Validation.REQUIRED_FIELD);
        RuleFor(x => x.Title)
            .NotEmpty().WithErrorCode(ApplicationErrors.Validation.REQUIRED_FIELD)
            .MaximumLength(Post.MaxTitleLength).WithErrorCode(ApplicationErrors.Validation.MAX_LENGTH);
        RuleFor(x => x.Content)
            .MaximumLength(Post.MaxContentLength).WithErrorCode(ApplicationErrors.Validation.MAX_LENGTH);
    }
}
