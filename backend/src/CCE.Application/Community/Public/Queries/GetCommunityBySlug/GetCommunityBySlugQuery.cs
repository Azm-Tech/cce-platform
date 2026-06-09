using CCE.Application.Common;
using CCE.Application.Community.Public.Dtos;
using MediatR;

namespace CCE.Application.Community.Public.Queries.GetCommunityBySlug;

public sealed record GetCommunityBySlugQuery(string Slug) : IRequest<Response<CommunityDto>>;
