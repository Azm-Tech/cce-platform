using System.Collections.Generic;

namespace CCE.Application.Community;

/// <summary>
/// Allow-list and limits for post attachments (§8). Documents are capped at 2 MB; media uses the
/// platform default. Enforced at the create-post boundary; the AssetFile domain stays generic.
/// </summary>
public static class PostAttachmentPolicy
{
    public const long MaxDocumentSizeBytes = 2 * 1024 * 1024;

    public static readonly IReadOnlySet<string> MediaMimeTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "image/png", "image/jpeg", "image/webp", "image/gif", "video/mp4",
    };

    public static readonly IReadOnlySet<string> DocumentMimeTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", // xlsx
        "application/msword",                                                 // doc
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document", // docx
    };
}
