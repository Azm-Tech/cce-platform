namespace CCE.Application.Media;

public sealed class MediaUploadOptions
{
    public const string SectionName = "Media";

    public string BaseUrl { get; init; } = "http://localhost:5001/media/";

    public long MaxSizeBytes { get; init; } = 52_428_800;

    public IReadOnlyList<string> AllowedMimeTypes { get; init; } = new[]
    {
        // Images
        "image/jpeg", "image/jpg", "image/png", "image/webp",
        // Video
        "video/mp4", "video/quicktime",
        // Documents
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-powerpoint",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
    };
}
