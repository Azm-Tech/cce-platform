using CCE.Domain.Identity;
using CCE.Domain.Identity.Events;
using CCE.Infrastructure.Persistence;
using CCE.Infrastructure.Persistence.Interceptors;
using CCE.TestInfrastructure.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace CCE.Infrastructure.Tests.Persistence;

public class DomainEventDispatcherTests
{
    private static (CceDbContext Ctx, IPublisher Publisher) Build()
    {
        var publisher = Substitute.For<IPublisher>();
        var dispatcher = new DomainEventDispatcher(publisher);
        var options = new DbContextOptionsBuilder<CceDbContext>()
            .UseInMemoryDatabase(System.Guid.NewGuid().ToString())
            .AddInterceptors(dispatcher)
            .Options;
        return (new CceDbContext(options), publisher);
    }

    [Fact]
    public async Task Saved_aggregate_with_event_publishes_it()
    {
        var (ctx, publisher) = Build();
        var clock = new FakeSystemClock();
        var req = ExpertRegistrationRequest.Submit(
            System.Guid.NewGuid(), "خبير", "Expert", new[] { "Solar" }, clock);
        req.Approve(System.Guid.NewGuid(), clock);

        ctx.ExpertRegistrationRequests.Add(req);
        await ctx.SaveChangesAsync();

        await publisher.Received(1).Publish(
            Arg.Any<ExpertRegistrationApprovedEvent>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DomainEvents_cleared_after_publish()
    {
        var (ctx, _) = Build();
        var clock = new FakeSystemClock();
        var req = ExpertRegistrationRequest.Submit(
            System.Guid.NewGuid(), "خبير", "Expert", new[] { "Solar" }, clock);
        req.Approve(System.Guid.NewGuid(), clock);
        ctx.ExpertRegistrationRequests.Add(req);

        await ctx.SaveChangesAsync();

        req.DomainEvents.Should().BeEmpty();
    }
}
