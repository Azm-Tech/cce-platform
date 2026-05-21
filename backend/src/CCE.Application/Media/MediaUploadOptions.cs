namespace CCE.Application.Media;

public sealed class MediaUploadOptions
{
    public const string SectionName = "Media";

    public string BaseUrl { get; init; } = "http://localhost:5001/media/";

    public long MaxSizeBytes { get; init; } = 52_428_800;

    public IReadOnlyList<string> AllowedMimeTypes { get; init; } = new[]
    {
        "image/png", "image/jpeg", "image/gif", "image/svg+xml", "image/webp",
        "video/mp4", "video/webm",
        "application/pdf", "text/csv", "text/plain", "application/zip",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel", "application/msword"
    };
}
