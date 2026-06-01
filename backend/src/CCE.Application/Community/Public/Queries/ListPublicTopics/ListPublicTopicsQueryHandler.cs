using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Community.Public.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Community;
using MediatR;

namespace CCE.Application.Community.Public.Queries.ListPublicTopics;

public sealed class ListPublicTopicsQueryHandler
    : IRequestHandler<ListPublicTopicsQuery, Response<System.Collections.Generic.IReadOnlyList<PublicTopicDto>>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _messages;

    public ListPublicTopicsQueryHandler(ICceDbContext db, MessageFactory messages)
    {
        _db = db;
        _messages = messages;
    }

    public async Task<Response<System.Collections.Generic.IReadOnlyList<PublicTopicDto>>> Handle(
        ListPublicTopicsQuery request,
        CancellationToken cancellationToken)
    {
        var list = await _db.Topics
            .Where(t => t.IsActive)
            .OrderBy(t => t.OrderIndex)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        return _messages.Ok((System.Collections.Generic.IReadOnlyList<PublicTopicDto>)list.Select(MapToDto).ToList(), "ITEMS_LISTED");
    }

    internal static PublicTopicDto MapToDto(Topic t) => new(
        t.Id,
        t.NameAr,
        t.NameEn,
        t.DescriptionAr,
        t.DescriptionEn,
        t.Slug,
        t.ParentId,
        t.IconUrl,
        t.OrderIndex);
}
