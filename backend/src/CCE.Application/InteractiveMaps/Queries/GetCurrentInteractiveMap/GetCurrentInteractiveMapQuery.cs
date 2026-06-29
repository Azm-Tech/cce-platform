using CCE.Application.Common;
using CCE.Application.InteractiveMaps.Dtos;
using MediatR;

namespace CCE.Application.InteractiveMaps.Queries.GetCurrentInteractiveMap;

public sealed record GetCurrentInteractiveMapQuery : IRequest<Response<InteractiveMapDto>>;
