using CCE.Application.Common;
using CCE.Application.InteractiveMaps.Public.Dtos;
using MediatR;

namespace CCE.Application.InteractiveMaps.Public.Queries.GetInteractiveMapNodeDetails;

public sealed record GetInteractiveMapNodeDetailsQuery(System.Guid NodeId) : IRequest<Response<MapNodeDetailsDto>>;
