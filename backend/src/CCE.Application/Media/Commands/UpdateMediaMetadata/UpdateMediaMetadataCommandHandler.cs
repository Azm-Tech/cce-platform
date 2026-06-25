using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Media.Dtos;
using CCE.Application.Messages;
using MediatR;

namespace CCE.Application.Media.Commands.UpdateMediaMetadata;

internal sealed class UpdateMediaMetadataCommandHandler
    : IRequestHandler<UpdateMediaMetadataCommand, Response<MediaFileBriefDto>>
{
    private readonly IMediaFileRepository _repo;
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public UpdateMediaMetadataCommandHandler(
        IMediaFileRepository repo,
        ICceDbContext db,
        MessageFactory msg)
    {
        _repo = repo;
        _db = db;
        _msg = msg;
    }

    public async Task<Response<MediaFileBriefDto>> Handle(
        UpdateMediaMetadataCommand request, CancellationToken ct)
    {
        var mediaFile = await _repo.FindAsync(request.Id, ct).ConfigureAwait(false);
        if (mediaFile is null)
            return _msg.NotFound<MediaFileBriefDto>(MessageKeys.Media.MEDIA_FILE_NOT_FOUND);

        mediaFile.UpdateMetadata(
            request.TitleAr,
            request.TitleEn,
            request.DescriptionAr,
            request.DescriptionEn,
            request.AltTextAr,
            request.AltTextEn);

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        var dto = new MediaFileBriefDto(mediaFile.Id, mediaFile.StorageKey, mediaFile.Url);
        return _msg.Ok(dto, MessageKeys.Media.MEDIA_UPDATED);
    }
}
