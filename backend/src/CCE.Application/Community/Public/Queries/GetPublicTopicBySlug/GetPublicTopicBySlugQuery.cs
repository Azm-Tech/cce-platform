using CCE.Application.Common;
using CCE.Application.Community.Public.Dtos;
using MediatR;

namespace CCE.Application.Community.Public.Queries.GetPublicTopicBySlug;

public sealed record GetPublicTopicBySlugQuery(string Slug) : IRequest<Response<PublicTopicDto>>;
