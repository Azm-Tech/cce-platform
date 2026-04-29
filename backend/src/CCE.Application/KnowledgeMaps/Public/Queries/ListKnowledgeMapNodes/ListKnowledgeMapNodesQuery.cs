using CCE.Application.KnowledgeMaps.Public.Dtos;
using MediatR;

namespace CCE.Application.KnowledgeMaps.Public.Queries.ListKnowledgeMapNodes;

public sealed record ListKnowledgeMapNodesQuery(System.Guid MapId)
    : IRequest<IReadOnlyList<PublicKnowledgeMapNodeDto>>;
