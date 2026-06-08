using CCE.Application.Errors;
using CCE.Domain.Community;
using FluentValidation;

namespace CCE.Application.Community.Commands.CreatePost;

public sealed class CreatePostCommandValidator : AbstractValidator<CreatePostCommand>
{
    public CreatePostCommandValidator()
    {
        RuleFor(x => x.CommunityId).NotEmpty().WithErrorCode(ApplicationErrors.Validation.REQUIRED_FIELD);
        RuleFor(x => x.TopicId).NotEmpty().WithErrorCode(ApplicationErrors.Validation.REQUIRED_FIELD);
        RuleFor(x => x.Type).IsInEnum().WithErrorCode(ApplicationErrors.Validation.INVALID_ENUM);
        RuleFor(x => x.Title)
            .NotEmpty().WithErrorCode(ApplicationErrors.Validation.REQUIRED_FIELD)
            .MaximumLength(Post.MaxTitleLength).WithErrorCode(ApplicationErrors.Validation.MAX_LENGTH);
        RuleFor(x => x.Content)
            .MaximumLength(Post.MaxContentLength).WithErrorCode(ApplicationErrors.Validation.MAX_LENGTH);
        RuleFor(x => x.Locale)
            .Must(l => l is "ar" or "en").WithErrorCode(ApplicationErrors.Validation.INVALID_ENUM);
    }
}
