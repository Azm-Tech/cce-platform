using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Community.Public.Dtos;
using CCE.Domain.Community;
using MediatR;

namespace CCE.Application.Community.Public.Queries.ListPublicTopics;

public sealed class ListPublicTopicsQueryHandler
    : IRequestHandler<ListPublicTopicsQuery, System.Collections.Generic.IReadOnlyList<PublicTopicDto>>
{
    private readonly ICceDbContext _db;

    public ListPublicTopicsQueryHandler(ICceDbContext db)
    {
        _db = db;
    }

    public async Task<System.Collections.Generic.IReadOnlyList<PublicTopicDto>> Handle(
        ListPublicTopicsQuery request,
        CancellationToken cancellationToken)
    {
        var list = await _db.Topics
            .Where(t => t.IsActive)
            .OrderBy(t => t.OrderIndex)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        return list.Select(MapToDto).ToList();
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
