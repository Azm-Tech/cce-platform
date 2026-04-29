using CCE.Application.Content.Public.Dtos;
using MediatR;

namespace CCE.Application.Content.Public.Queries.GetPublicPageBySlug;

public sealed record GetPublicPageBySlugQuery(string Slug) : IRequest<PublicPageDto?>;
