using CCE.Application.Content.Dtos;
using MediatR;

namespace CCE.Application.Content.Commands.UploadAsset;

/// <summary>
/// Stores the uploaded file via <c>IFileStorage</c>, scans via <c>IClamAvScanner</c>,
/// then persists an <c>AssetFile</c> row with the terminal scan status.
/// On Infected, the storage object is deleted before persisting the row.
/// </summary>
public sealed record UploadAssetCommand(
    Stream Content,
    string OriginalFileName,
    string MimeType,
    long SizeBytes) : IRequest<AssetFileDto>;
