using CCE.Domain.Community;

namespace CCE.Application.Community.Commands.CreatePost;

/// <summary>
/// One attachment to link to a post. The asset was already uploaded via POST /api/assets;
/// the client echoes back the MimeType and SizeBytes from the upload response so the
/// create-post validator can enforce type and size rules without a second DB round-trip.
/// </summary>
public sealed record PostAttachmentInput(
    Guid AssetFileId,
    AttachmentKind Kind,
    int SortOrder,
    string? MetadataJson,
    string MimeType,
    long SizeBytes);
