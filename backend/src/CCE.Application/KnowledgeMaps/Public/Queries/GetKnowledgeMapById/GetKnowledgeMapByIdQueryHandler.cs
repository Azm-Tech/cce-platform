using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Messages;
using CCE.Application.KnowledgeMaps.Public.Dtos;
using CCE.Application.KnowledgeMaps.Public.Queries.ListKnowledgeMaps;

using MediatR;

namespace CCE.Application.KnowledgeMaps.Public.Queries.GetKnowledgeMapById;

public sealed class GetKnowledgeMapByIdQueryHandler
    : IRequestHandler<GetKnowledgeMapByIdQuery, Response<PublicKnowledgeMapDto>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public GetKnowledgeMapByIdQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<PublicKnowledgeMapDto>> Handle(
        GetKnowledgeMapByIdQuery request, CancellationToken cancellationToken)
    {
        var list = await _db.KnowledgeMaps
            .Where(m => m.Id == request.Id)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        var map = list.SingleOrDefault();
        if (map is null)
            return _msg.NotFound<PublicKnowledgeMapDto>(MessageKeys.KnowledgeMap.MAP_NOT_FOUND);
        return _msg.Ok(ListKnowledgeMapsQueryHandler.MapToDto(map), MessageKeys.General.SUCCESS_OPERATION);
    }
}
