using CCE.Application.Common;
using CCE.Application.Common.Behaviors;
using CCE.Application.Common.Caching;
using CCE.Domain.Common;
using FluentAssertions;
using MediatR;
using NSubstitute;
using Xunit;

namespace CCE.Infrastructure.Tests.Caching;

public sealed class CacheInvalidationBehaviorTests
{
    private sealed record InvalidatingRequest(IReadOnlyCollection<string> CacheRegionsToEvict)
        : IRequest<Response<VoidData>>, ICacheInvalidatingRequest;

    private sealed record PlainRequest : IRequest<Response<VoidData>>;

    [Fact]
    public async Task Evicts_declared_regions_on_success()
    {
        var invalidator = Substitute.For<IOutputCacheInvalidator>();
        var behavior = new CacheInvalidationBehavior<InvalidatingRequest, Response<VoidData>>(invalidator);
        var request = new InvalidatingRequest([CacheRegions.Resources, CacheRegions.Feed]);
        RequestHandlerDelegate<Response<VoidData>> next = () => Task.FromResult(Response.Ok("CON900", "ok"));

        await behavior.Handle(request, next, CancellationToken.None);

        await invalidator.Received(1).EvictRegionsAsync(
            Arg.Is<IEnumerable<string>>(r => r.Contains(CacheRegions.Resources) && r.Contains(CacheRegions.Feed)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Does_not_evict_when_response_failed()
    {
        var invalidator = Substitute.For<IOutputCacheInvalidator>();
        var behavior = new CacheInvalidationBehavior<InvalidatingRequest, Response<VoidData>>(invalidator);
        var request = new InvalidatingRequest([CacheRegions.Resources]);
        RequestHandlerDelegate<Response<VoidData>> next =
            () => Task.FromResult(Response.Fail("ERR900", "boom", MessageType.BusinessRule));

        await behavior.Handle(request, next, CancellationToken.None);

        await invalidator.DidNotReceive().EvictRegionsAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Ignores_requests_that_do_not_invalidate()
    {
        var invalidator = Substitute.For<IOutputCacheInvalidator>();
        var behavior = new CacheInvalidationBehavior<PlainRequest, Response<VoidData>>(invalidator);
        RequestHandlerDelegate<Response<VoidData>> next = () => Task.FromResult(Response.Ok("CON900", "ok"));

        await behavior.Handle(new PlainRequest(), next, CancellationToken.None);

        await invalidator.DidNotReceive().EvictRegionsAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>());
    }
}
