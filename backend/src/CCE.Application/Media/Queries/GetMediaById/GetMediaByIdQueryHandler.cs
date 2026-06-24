using CCE.Application.Common;
using CCE.Application.Media.Dtos;
using CCE.Application.Messages;
using MediatR;

namespace CCE.Application.Media.Queries.GetMediaById;

internal sealed class GetMediaByIdQueryHandler
    : IRequestHandler<GetMediaByIdQuery, Response<MediaFileDto>>
{
    private readonly IMediaFileRepository _repo;
    private readonly MessageFactory _msg;

    public GetMediaByIdQueryHandler(
        IMediaFileRepository repo,
        MessageFactory msg)
    {
        _repo = repo;
        _msg = msg;
    }

    public async Task<Response<MediaFileDto>> Handle(
        GetMediaByIdQuery request, CancellationToken ct)
    {
        var mediaFile = await _repo.FindAsync(request.Id, ct).ConfigureAwait(false);
        if (mediaFile is null)
            return _msg.MediaFileNotFound<MediaFileDto>();

        return _msg.Ok(MediaFileDto.FromEntity(mediaFile), MessageKeys.General.ITEMS_LISTED);
    }
}
