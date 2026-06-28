using CCE.Application.Common;
using CCE.Application.Common.Caching;
using CCE.Application.Content.Dtos;
using MediatR;

namespace CCE.Application.Content.Commands.PublishResource;

public sealed record PublishResourceCommand(System.Guid Id)
    : IRequest<Response<System.Guid>>, ICacheInvalidatingRequest
{
    public IReadOnlyCollection<string> CacheRegionsToEvict { get; } =
        [CacheRegions.Resources, CacheRegions.Feed];
}
