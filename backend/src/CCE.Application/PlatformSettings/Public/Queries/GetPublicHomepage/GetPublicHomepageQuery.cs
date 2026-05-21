using CCE.Application.Common;
using CCE.Application.PlatformSettings.Public.Dtos;
using MediatR;

namespace CCE.Application.PlatformSettings.Public.Queries.GetPublicHomepage;

public sealed record GetPublicHomepageQuery() : IRequest<Response<PublicHomepageDto>>;
