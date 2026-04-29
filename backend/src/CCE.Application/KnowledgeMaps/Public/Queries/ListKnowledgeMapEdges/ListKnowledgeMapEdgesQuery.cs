using CCE.Application.KnowledgeMaps.Public.Dtos;
using MediatR;

namespace CCE.Application.KnowledgeMaps.Public.Queries.ListKnowledgeMapEdges;

public sealed record ListKnowledgeMapEdgesQuery(System.Guid MapId)
    : IRequest<IReadOnlyList<PublicKnowledgeMapEdgeDto>>;
