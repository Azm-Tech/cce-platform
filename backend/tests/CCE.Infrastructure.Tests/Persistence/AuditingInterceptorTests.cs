using CCE.Application.Common.Interfaces;
using CCE.Domain.Audit;
using CCE.Domain.Common;
using CCE.Infrastructure.Persistence;
using CCE.Infrastructure.Persistence.Interceptors;
using CCE.TestInfrastructure.Time;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace CCE.Infrastructure.Tests.Persistence;

public class AuditingInterceptorTests
{
    private static (CceDbContext Ctx, FakeSystemClock Clock, ICurrentUserAccessor Accessor) Build()
    {
        var clock = new FakeSystemClock();
        var accessor = Substitute.For<ICurrentUserAccessor>();
        accessor.GetActor().Returns("user:test");
        accessor.GetCorrelationId().Returns(System.Guid.NewGuid());
        var interceptor = new AuditingInterceptor(accessor, clock);
        var options = new DbContextOptionsBuilder<CceDbContext>()
            .UseInMemoryDatabase(System.Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;
        var ctx = new CceDbContext(options);
        return (ctx, clock, accessor);
    }

    [Fact]
    public async Task Saving_audited_entity_writes_an_AuditEvent_in_same_transaction()
    {
        var (ctx, clock, _) = Build();
        var country = CCE.Domain.Country.Country.Register(
            "SAU", "SA", "السعودية", "Saudi Arabia", "آسيا", "Asia",
            "https://flags.example/sa.svg");
        ctx.Countries.Add(country);

        await ctx.SaveChangesAsync();

        var events = ctx.AuditEvents.AsNoTracking().ToList();
        events.Should().HaveCount(1);
        events[0].Actor.Should().Be("user:test");
        events[0].Action.Should().Be("Country.Added");
        events[0].Resource.Should().Be($"Country/{country.Id}");
        events[0].OccurredOn.Should().Be(clock.UtcNow);
    }

    [Fact]
    public async Task Modifying_entity_diffs_changed_properties_only()
    {
        var (ctx, _, _) = Build();
        var country = CCE.Domain.Country.Country.Register(
            "SAU", "SA", "السعودية", "Saudi Arabia", "آسيا", "Asia",
            "https://flags.example/sa.svg");
        ctx.Countries.Add(country);
        await ctx.SaveChangesAsync();

        ctx.AuditEvents.RemoveRange(ctx.AuditEvents);
        await ctx.SaveChangesAsync();

        country.Deactivate();
        await ctx.SaveChangesAsync();

        var modifyEvents = ctx.AuditEvents.AsNoTracking()
            .Where(e => e.Action == "Country.Modified").ToList();
        modifyEvents.Should().HaveCount(1);
        modifyEvents[0].Diff.Should().Contain("IsActive");
        modifyEvents[0].Diff.Should().NotContain("\"NameAr\"");
    }

    [Fact]
    public async Task Saving_a_non_audited_entity_writes_no_AuditEvent()
    {
        var (ctx, _, _) = Build();
        var result = CCE.Domain.InteractiveCity.CityScenarioResult.Compute(
            System.Guid.NewGuid(), 2050, 100m, "v1", new FakeSystemClock());
        ctx.CityScenarioResults.Add(result);

        await ctx.SaveChangesAsync();

        ctx.AuditEvents.Should().BeEmpty();
    }
}
