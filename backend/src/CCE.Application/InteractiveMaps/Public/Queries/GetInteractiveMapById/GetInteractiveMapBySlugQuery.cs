using CCE.Application.Common;
using CCE.Application.InteractiveMaps.Public.Dtos;
using MediatR;

namespace CCE.Application.InteractiveMaps.Public.Queries.GetInteractiveMapById;

public sealed record GetPublicInteractiveMapByIdQuery(System.Guid Id) : IRequest<Response<PublicInteractiveMapDto>>;
