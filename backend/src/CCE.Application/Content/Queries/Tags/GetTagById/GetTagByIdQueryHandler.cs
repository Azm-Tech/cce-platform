using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Dtos;
using CCE.Application.Messages;
using MediatR;

namespace CCE.Application.Content.Queries.Tags.GetTagById;

public sealed class GetTagByIdQueryHandler : IRequestHandler<GetTagByIdQuery, Response<TagDto>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _messages;

    public GetTagByIdQueryHandler(ICceDbContext db, MessageFactory messages)
    {
        _db = db;
        _messages = messages;
    }

    public async Task<Response<TagDto>> Handle(GetTagByIdQuery request, CancellationToken cancellationToken)
    {
        var tags = await _db.Tags.Where(t => t.Id == request.Id)
            .ToListAsyncEither(cancellationToken).ConfigureAwait(false);
        var tag = tags.FirstOrDefault();
        if (tag is null)
            return _messages.NotFound<TagDto>(MessageKeys.Content.TAG_NOT_FOUND);

        return _messages.Ok(new TagDto(tag.Id, tag.NameAr, tag.NameEn, tag.Color), MessageKeys.General.SUCCESS_OPERATION);
    }
}
