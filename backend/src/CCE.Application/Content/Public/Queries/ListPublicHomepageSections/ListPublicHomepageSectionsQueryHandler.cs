using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Public.Dtos;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Public.Queries.ListPublicHomepageSections;

public sealed class ListPublicHomepageSectionsQueryHandler
    : IRequestHandler<ListPublicHomepageSectionsQuery, System.Collections.Generic.IReadOnlyList<PublicHomepageSectionDto>>
{
    private readonly ICceDbContext _db;

    public ListPublicHomepageSectionsQueryHandler(ICceDbContext db)
    {
        _db = db;
    }

    public async Task<System.Collections.Generic.IReadOnlyList<PublicHomepageSectionDto>> Handle(
        ListPublicHomepageSectionsQuery request,
        CancellationToken cancellationToken)
    {
        var list = await _db.HomepageSections
            .Where(s => s.IsActive)
            .OrderBy(s => s.OrderIndex)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        return list.Select(MapToDto).ToList();
    }

    internal static PublicHomepageSectionDto MapToDto(HomepageSection s) => new(
        s.Id,
        s.SectionType,
        s.OrderIndex,
        s.ContentAr,
        s.ContentEn);
}
