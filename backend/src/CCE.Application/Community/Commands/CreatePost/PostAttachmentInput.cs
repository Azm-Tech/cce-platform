using CCE.Domain.Community;

namespace CCE.Application.Community.Commands.CreatePost;

/// <summary>One attachment to link to a post; the asset was already uploaded via the asset pipeline.</summary>
public sealed record PostAttachmentInput(
    Guid AssetFileId,
    AttachmentKind Kind,
    int SortOrder,
    string? MetadataJson);
