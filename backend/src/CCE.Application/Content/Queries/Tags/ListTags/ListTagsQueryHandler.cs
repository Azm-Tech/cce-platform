using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Dtos;
using CCE.Application.Messages;
using MediatR;

namespace CCE.Application.Content.Queries.Tags.ListTags;

public sealed class ListTagsQueryHandler : IRequestHandler<ListTagsQuery, Response<System.Collections.Generic.List<TagDto>>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _messages;

    public ListTagsQueryHandler(ICceDbContext db, MessageFactory messages)
    {
        _db = db;
        _messages = messages;
    }

    public async Task<Response<System.Collections.Generic.List<TagDto>>> Handle(ListTagsQuery request, CancellationToken cancellationToken)
    {
        var tags = await _db.Tags
            .OrderBy(t => t.NameEn)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        var dtos = tags.Select(t => new TagDto(t.Id, t.NameAr, t.NameEn, t.Color)).ToList();
        return _messages.Ok(dtos, "ITEMS_LISTED");
    }
}
