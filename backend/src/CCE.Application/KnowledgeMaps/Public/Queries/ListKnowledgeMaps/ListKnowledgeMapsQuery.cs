using CCE.Application.KnowledgeMaps.Public.Dtos;
using MediatR;

namespace CCE.Application.KnowledgeMaps.Public.Queries.ListKnowledgeMaps;

public sealed record ListKnowledgeMapsQuery : IRequest<IReadOnlyList<PublicKnowledgeMapDto>>;
