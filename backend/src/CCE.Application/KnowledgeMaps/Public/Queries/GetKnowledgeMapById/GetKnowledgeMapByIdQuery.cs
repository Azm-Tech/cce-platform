using CCE.Application.KnowledgeMaps.Public.Dtos;
using MediatR;

namespace CCE.Application.KnowledgeMaps.Public.Queries.GetKnowledgeMapById;

public sealed record GetKnowledgeMapByIdQuery(System.Guid Id) : IRequest<PublicKnowledgeMapDto?>;
