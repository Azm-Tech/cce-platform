using CCE.Application.Common;
using CCE.Application.InteractiveMaps.Public.Dtos;
using MediatR;

namespace CCE.Application.InteractiveMaps.Public.Queries.ListInteractiveMaps;

public sealed record ListInteractiveMapsQuery : IRequest<Response<IReadOnlyList<PublicInteractiveMapDto>>>;
