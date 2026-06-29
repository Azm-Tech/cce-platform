using System.Collections.Generic;
using System.Linq;

namespace CCE.Application.Community;

/// <summary>
/// Allow-list and per-type limits for post attachments. Enforced at the create-post boundary;
/// the AssetFile domain stays generic.
/// </summary>
public static class PostAttachmentPolicy
{
    // ── Count limits ────────────────────────────────────────────────────────
    public const int MaxImageCount    = 5;
    public const int MaxVideoCount    = 1;
    public const int MaxDocumentCount = 3;

    // ── Size limits ─────────────────────────────────────────────────────────
    public const long MaxImageSizeBytes    =   5L * 1024 * 1024;  // 5 MB
    public const long MaxVideoSizeBytes    = 100L * 1024 * 1024;  // 100 MB
    public const long MaxDocumentSizeBytes =  10L * 1024 * 1024;  // 10 MB

    // ── MIME allow-lists ────────────────────────────────────────────────────
    public static readonly IReadOnlySet<string> ImageMimeTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp",
    };

    public static readonly IReadOnlySet<string> VideoMimeTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "video/mp4", "video/quicktime",  // quicktime = MOV
    };

    /// <summary>Union of image + video MIME types (AttachmentKind.Media).</summary>
    public static readonly IReadOnlySet<string> AllMediaMimeTypes =
        new HashSet<string>(ImageMimeTypes.Concat(VideoMimeTypes), StringComparer.OrdinalIgnoreCase);

    public static readonly IReadOnlySet<string> DocumentMimeTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "application/msword",                                                          // doc
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",    // docx
        "application/vnd.ms-powerpoint",                                               // ppt
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",  // pptx
        "application/vnd.ms-excel",                                                    // xls
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",          // xlsx
    };
}
