namespace CCE.Application.Content.Dtos;

public sealed record DownloadFilePayload(
    System.IO.Stream Content,
    string MimeType,
    string OriginalFileName);
