using CCE.Application.Common;
using CCE.Application.Content;
using CCE.Application.Media.Dtos;
using CCE.Application.Messages;
using MediatR;

namespace CCE.Application.Media.Queries.GetMediaById;

internal sealed class GetMediaByIdQueryHandler
    : IRequestHandler<GetMediaByIdQuery, Response<MediaFileDto>>
{
    private readonly IMediaFileRepository _repo;
    private readonly IFileStorage _storage;
    private readonly MessageFactory _msg;

    public GetMediaByIdQueryHandler(
        IMediaFileRepository repo,
        IFileStorage storage,
        MessageFactory msg)
    {
        _repo = repo;
        _storage = storage;
        _msg = msg;
    }

    public async Task<Response<MediaFileDto>> Handle(
        GetMediaByIdQuery request, CancellationToken ct)
    {
        var mediaFile = await _repo.FindAsync(request.Id, ct).ConfigureAwait(false);
        if (mediaFile is null)
            return _msg.NotFound<MediaFileDto>(MessageKeys.Media.MEDIA_FILE_NOT_FOUND);

        var publicUrl = _storage.GetPublicUrl(mediaFile.Url).ToString();
        var dto = new MediaFileDto(
            mediaFile.Id, mediaFile.StorageKey, publicUrl,
            mediaFile.OriginalFileName, mediaFile.MimeType, mediaFile.SizeBytes,
            mediaFile.TitleAr, mediaFile.TitleEn,
            mediaFile.DescriptionAr, mediaFile.DescriptionEn,
            mediaFile.AltTextAr, mediaFile.AltTextEn,
            mediaFile.UploadedById, mediaFile.UploadedOn);
        return _msg.Ok(dto, MessageKeys.General.ITEMS_LISTED);
    }
}
