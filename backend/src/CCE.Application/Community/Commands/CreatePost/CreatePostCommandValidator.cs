using CCE.Application.Community;
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

        // MimeType and SizeBytes are echoed from the upload response — no DB needed.
        RuleFor(x => x.Attachments)
            .Must(a => a.Count <= Post.MaxAttachments)
            .WithErrorCode(MessageKeys.Media.FILE_TOO_LARGE)
            .When(x => x.Attachments.Count > 0);

        RuleFor(x => x.Attachments)
            .Must(a => a.Count(att => PostAttachmentPolicy.ImageMimeTypes.Contains(att.MimeType)) <= PostAttachmentPolicy.MaxImageCount)
            .WithErrorCode(MessageKeys.Media.IMAGE_LIMIT_EXCEEDED)
            .When(x => x.Attachments.Count > 0);

        RuleFor(x => x.Attachments)
            .Must(a => a.Count(att => PostAttachmentPolicy.VideoMimeTypes.Contains(att.MimeType)) <= PostAttachmentPolicy.MaxVideoCount)
            .WithErrorCode(MessageKeys.Media.VIDEO_LIMIT_EXCEEDED)
            .When(x => x.Attachments.Count > 0);

        RuleFor(x => x.Attachments)
            .Must(a => a.Count(att => att.Kind == AttachmentKind.Document) <= PostAttachmentPolicy.MaxDocumentCount)
            .WithErrorCode(MessageKeys.Media.FILE_LIMIT_EXCEEDED)
            .When(x => x.Attachments.Count > 0);

        RuleForEach(x => x.Attachments)
            .Must(att =>
            {
                var allowed = att.Kind == AttachmentKind.Media
                    ? PostAttachmentPolicy.AllMediaMimeTypes
                    : PostAttachmentPolicy.DocumentMimeTypes;
                return allowed.Contains(att.MimeType);
            })
            .WithErrorCode(MessageKeys.Media.INVALID_FILE_TYPE)
            .When(x => x.Attachments.Count > 0);

        RuleForEach(x => x.Attachments)
            .Must(att =>
            {
                var max = PostAttachmentPolicy.ImageMimeTypes.Contains(att.MimeType)
                    ? PostAttachmentPolicy.MaxImageSizeBytes
                    : PostAttachmentPolicy.VideoMimeTypes.Contains(att.MimeType)
                        ? PostAttachmentPolicy.MaxVideoSizeBytes
                        : PostAttachmentPolicy.MaxDocumentSizeBytes;
                return att.SizeBytes <= max;
            })
            .WithErrorCode(MessageKeys.Media.FILE_TOO_LARGE)
            .When(x => x.Attachments.Count > 0);
    }
}
