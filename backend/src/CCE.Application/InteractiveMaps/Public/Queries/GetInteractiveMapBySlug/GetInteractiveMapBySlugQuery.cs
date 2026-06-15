using CCE.Application.Common;
using CCE.Application.InteractiveMaps.Public.Dtos;
using MediatR;

namespace CCE.Application.InteractiveMaps.Public.Queries.GetInteractiveMapBySlug;

public sealed record GetInteractiveMapBySlugQuery(string Slug) : IRequest<Response<PublicInteractiveMapDto>>;
