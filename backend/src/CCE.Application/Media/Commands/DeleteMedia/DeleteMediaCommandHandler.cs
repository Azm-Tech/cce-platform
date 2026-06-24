using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Content;
using CCE.Application.Media.Dtos;
using CCE.Application.Messages;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace CCE.Application.Media.Commands.DeleteMedia;

internal sealed class DeleteMediaCommandHandler
    : IRequestHandler<DeleteMediaCommand, Response<MediaFileBriefDto>>
{
    private readonly IMediaFileRepository _repo;
    private readonly IFileStorage _fileStorage;
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public DeleteMediaCommandHandler(
        IMediaFileRepository repo,
        [FromKeyedServices("media")] IFileStorage fileStorage,
        ICceDbContext db,
        MessageFactory msg)
    {
        _repo = repo;
        _fileStorage = fileStorage;
        _db = db;
        _msg = msg;
    }

    public async Task<Response<MediaFileBriefDto>> Handle(
        DeleteMediaCommand request, CancellationToken ct)
    {
        var mediaFile = await _repo.FindAsync(request.Id, ct).ConfigureAwait(false);
        if (mediaFile is null)
            return _msg.MediaFileNotFound<MediaFileBriefDto>();

        await _fileStorage.DeleteAsync(mediaFile.StorageKey, ct).ConfigureAwait(false);

        _db.Delete(mediaFile);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        var dto = new MediaFileBriefDto(mediaFile.Id, mediaFile.StorageKey, mediaFile.Url);
        return _msg.Ok(dto, MessageKeys.Media.MEDIA_DELETED);
    }
}
