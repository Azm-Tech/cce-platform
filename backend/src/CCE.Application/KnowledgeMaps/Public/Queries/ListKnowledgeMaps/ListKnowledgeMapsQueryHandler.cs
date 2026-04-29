using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.KnowledgeMaps.Public.Dtos;
using CCE.Domain.KnowledgeMaps;
using MediatR;

namespace CCE.Application.KnowledgeMaps.Public.Queries.ListKnowledgeMaps;

public sealed class ListKnowledgeMapsQueryHandler
    : IRequestHandler<ListKnowledgeMapsQuery, IReadOnlyList<PublicKnowledgeMapDto>>
{
    private readonly ICceDbContext _db;

    public ListKnowledgeMapsQueryHandler(ICceDbContext db) => _db = db;

    public async Task<IReadOnlyList<PublicKnowledgeMapDto>> Handle(
        ListKnowledgeMapsQuery request, CancellationToken cancellationToken)
    {
        var items = await _db.KnowledgeMaps
            .OrderBy(m => m.NameEn)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        return items.Select(MapToDto).ToList();
    }

    internal static PublicKnowledgeMapDto MapToDto(KnowledgeMap m) => new(
        m.Id,
        m.NameAr,
        m.NameEn,
        m.DescriptionAr,
        m.DescriptionEn,
        m.Slug,
        m.IsActive);
}
