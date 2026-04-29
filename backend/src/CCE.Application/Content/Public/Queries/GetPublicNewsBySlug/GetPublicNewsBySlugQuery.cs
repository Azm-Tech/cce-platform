using CCE.Application.Content.Public.Dtos;
using MediatR;

namespace CCE.Application.Content.Public.Queries.GetPublicNewsBySlug;

public sealed record GetPublicNewsBySlugQuery(string Slug) : IRequest<PublicNewsDto?>;
