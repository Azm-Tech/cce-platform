using CCE.Application.Common;
using CCE.Application.Content.Public.Dtos;
using MediatR;

namespace CCE.Application.Content.Public.Queries.GetPublicNewsBySlug;

public sealed record GetPublicNewsBySlugQuery(string Slug) : IRequest<Response<PublicNewsDto>>;
