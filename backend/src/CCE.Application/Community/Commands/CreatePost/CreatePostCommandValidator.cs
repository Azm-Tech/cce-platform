using CCE.Application.Messages;
using CCE.Domain.Community;
using FluentValidation;

namespace CCE.Application.Community.Commands.CreatePost;

public sealed class CreatePostCommandValidator : AbstractValidator<CreatePostCommand>
{
    public CreatePostCommandValidator()
    {
        RuleFor(x => x.CommunityId).NotEmpty().WithErrorCode(MessageKeys.Validation.REQUIRED_FIELD);
        RuleFor(x => x.TopicId).NotEmpty().WithErrorCode(MessageKeys.Validation.REQUIRED_FIELD);
        RuleFor(x => x.Type).IsInEnum().WithErrorCode(MessageKeys.Validation.INVALID_ENUM);
        RuleFor(x => x.Title)
            .NotEmpty().WithErrorCode(MessageKeys.Validation.REQUIRED_FIELD)
            .MaximumLength(Post.MaxTitleLength).WithErrorCode(MessageKeys.Validation.MAX_LENGTH);
        RuleFor(x => x.Content)
            .MaximumLength(Post.MaxContentLength).WithErrorCode(MessageKeys.Validation.MAX_LENGTH);
        RuleFor(x => x.Locale)
            .Must(l => l is "ar" or "en").WithErrorCode(MessageKeys.Validation.INVALID_ENUM);
    }
}
