using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Dtos;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Queries.ListHomepageSections;

public sealed class ListHomepageSectionsQueryHandler
    : IRequestHandler<ListHomepageSectionsQuery, System.Collections.Generic.IReadOnlyList<HomepageSectionDto>>
{
    private readonly ICceDbContext _db;

    public ListHomepageSectionsQueryHandler(ICceDbContext db) => _db = db;

    public async Task<System.Collections.Generic.IReadOnlyList<HomepageSectionDto>> Handle(
        ListHomepageSectionsQuery request,
        CancellationToken cancellationToken)
    {
        var list = await _db.HomepageSections
            .OrderBy(s => s.OrderIndex)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);
        return list.Select(MapToDto).ToList();
    }

    internal static HomepageSectionDto MapToDto(HomepageSection s) => new(
        s.Id, s.SectionType, s.OrderIndex, s.ContentAr, s.ContentEn, s.IsActive);
}
