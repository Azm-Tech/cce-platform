using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.KnowledgeMaps.Public.Dtos;
using CCE.Application.KnowledgeMaps.Public.Queries.ListKnowledgeMaps;
using MediatR;

namespace CCE.Application.KnowledgeMaps.Public.Queries.GetKnowledgeMapById;

public sealed class GetKnowledgeMapByIdQueryHandler
    : IRequestHandler<GetKnowledgeMapByIdQuery, PublicKnowledgeMapDto?>
{
    private readonly ICceDbContext _db;

    public GetKnowledgeMapByIdQueryHandler(ICceDbContext db) => _db = db;

    public async Task<PublicKnowledgeMapDto?> Handle(
        GetKnowledgeMapByIdQuery request, CancellationToken cancellationToken)
    {
        var list = await _db.KnowledgeMaps
            .Where(m => m.Id == request.Id)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        var map = list.SingleOrDefault();
        return map is null ? null : ListKnowledgeMapsQueryHandler.MapToDto(map);
    }
}
