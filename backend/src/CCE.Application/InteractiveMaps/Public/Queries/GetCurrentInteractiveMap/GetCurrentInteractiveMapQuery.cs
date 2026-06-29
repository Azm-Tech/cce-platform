using CCE.Application.Common;
using CCE.Application.InteractiveMaps.Public.Dtos;
using MediatR;

namespace CCE.Application.InteractiveMaps.Public.Queries.GetCurrentInteractiveMap;

public sealed record GetCurrentInteractiveMapQuery : IRequest<Response<PublicInteractiveMapDto>>;
