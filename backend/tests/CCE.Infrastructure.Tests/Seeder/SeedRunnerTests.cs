using CCE.Infrastructure.Persistence;
using CCE.Seeder;
using CCE.Seeder.Seeders;
using CCE.TestInfrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CCE.Infrastructure.Tests.Seeder;

public class SeedRunnerTests
{
    private static (CceDbContext Ctx, SeedRunner Runner) Build()
    {
        var ctx = new CceDbContext(new DbContextOptionsBuilder<CceDbContext>()
            .UseInMemoryDatabase(System.Guid.NewGuid().ToString())
            .Options);
        var clock = new FakeSystemClock();
        var seeders = new ISeeder[]
        {
            new RolesAndPermissionsSeeder(ctx, NullLogger<RolesAndPermissionsSeeder>.Instance),
            new ReferenceDataSeeder(ctx, clock, NullLogger<ReferenceDataSeeder>.Instance),
            new KnowledgeMapSeeder(ctx, NullLogger<KnowledgeMapSeeder>.Instance),
            new DemoDataSeeder(ctx, clock, NullLogger<DemoDataSeeder>.Instance),
        };
        var runner = new SeedRunner(seeders, NullLogger<SeedRunner>.Instance);
        return (ctx, runner);
    }

    [Fact]
    public async Task RunAll_without_demo_seeds_reference_only()
    {
        var (ctx, runner) = Build();
        using (ctx)
        {
            await runner.RunAllAsync(includeDemo: false);
            (await ctx.Countries.CountAsync()).Should().BeGreaterThan(0);
            (await ctx.News.CountAsync()).Should().Be(0);
        }
    }

    [Fact]
    public async Task RunAll_with_demo_seeds_demo_data_too()
    {
        var (ctx, runner) = Build();
        using (ctx)
        {
            await runner.RunAllAsync(includeDemo: true);
            (await ctx.News.CountAsync()).Should().BeGreaterThan(0);
        }
    }
}
