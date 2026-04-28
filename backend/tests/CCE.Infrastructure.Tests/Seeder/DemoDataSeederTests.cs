using CCE.Infrastructure.Persistence;
using CCE.Seeder.Seeders;
using CCE.TestInfrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CCE.Infrastructure.Tests.Seeder;

public class DemoDataSeederTests
{
    private static (CceDbContext Ctx, DemoDataSeeder Seeder) Build()
    {
        var ctx = new CceDbContext(new DbContextOptionsBuilder<CceDbContext>()
            .UseInMemoryDatabase(System.Guid.NewGuid().ToString())
            .Options);
        return (ctx, new DemoDataSeeder(ctx, new FakeSystemClock(),
            NullLogger<DemoDataSeeder>.Instance));
    }

    [Fact]
    public async Task Seeds_news_and_event()
    {
        var (ctx, seeder) = Build();
        using (ctx)
        {
            await seeder.SeedAsync();
            (await ctx.News.CountAsync()).Should().BeGreaterThan(0);
            (await ctx.Events.CountAsync()).Should().BeGreaterThan(0);
        }
    }

    [Fact]
    public async Task News_articles_are_published()
    {
        var (ctx, seeder) = Build();
        using (ctx)
        {
            await seeder.SeedAsync();
            var allPublished = await ctx.News.AllAsync(n => n.PublishedOn != null);
            allPublished.Should().BeTrue();
        }
    }

    [Fact]
    public async Task Idempotent()
    {
        var (ctx, seeder) = Build();
        using (ctx)
        {
            await seeder.SeedAsync();
            var firstNews = await ctx.News.CountAsync();
            await seeder.SeedAsync();
            var secondNews = await ctx.News.CountAsync();
            secondNews.Should().Be(firstNews);
        }
    }
}
