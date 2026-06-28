using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.KnowledgeMaps.Public.Dtos;
using CCE.Application.Messages;
using CCE.Domain.KnowledgeMaps;
using MediatR;

namespace CCE.Application.KnowledgeMaps.Public.Queries.ListKnowledgeMaps;

public sealed class ListKnowledgeMapsQueryHandler
    : IRequestHandler<ListKnowledgeMapsQuery, Response<IReadOnlyList<PublicKnowledgeMapDto>>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public ListKnowledgeMapsQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<IReadOnlyList<PublicKnowledgeMapDto>>> Handle(
        ListKnowledgeMapsQuery request, CancellationToken cancellationToken)
    {
        var items = await _db.KnowledgeMaps
            .OrderBy(m => m.NameEn)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<PublicKnowledgeMapDto> list = items.Select(MapToDto).ToList();
        return _msg.Ok(list, MessageKeys.General.ITEMS_LISTED);
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
