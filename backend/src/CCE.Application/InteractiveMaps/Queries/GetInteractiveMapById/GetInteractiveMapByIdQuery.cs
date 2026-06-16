using CCE.Application.Common;
using CCE.Application.InteractiveMaps.Dtos;
using MediatR;

namespace CCE.Application.InteractiveMaps.Queries.GetInteractiveMapById;

public sealed record GetInteractiveMapByIdQuery(System.Guid Id) : IRequest<Response<InteractiveMapDto>>;
