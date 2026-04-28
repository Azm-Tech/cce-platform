using CCE.Infrastructure.Persistence;
using CCE.Seeder;
using CCE.Seeder.Seeders;
using CCE.TestInfrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CCE.Infrastructure.Tests.Seeder;

public class ReferenceDataSeederTests
{
    private static (CceDbContext Ctx, ReferenceDataSeeder Seeder) Build()
    {
        var ctx = new CceDbContext(new DbContextOptionsBuilder<CceDbContext>()
            .UseInMemoryDatabase(System.Guid.NewGuid().ToString())
            .Options);
        var seeder = new ReferenceDataSeeder(ctx, new FakeSystemClock(),
            NullLogger<ReferenceDataSeeder>.Instance);
        return (ctx, seeder);
    }

    [Fact]
    public async Task First_run_seeds_countries_categories_topics()
    {
        var (ctx, seeder) = Build();
        using (ctx)
        {
            await seeder.SeedAsync();
            (await ctx.Countries.CountAsync()).Should().BeGreaterThan(5);
            (await ctx.ResourceCategories.CountAsync()).Should().BeGreaterThan(3);
            (await ctx.Topics.CountAsync()).Should().BeGreaterThan(3);
        }
    }

    [Fact]
    public async Task Saudi_Arabia_seeded()
    {
        var (ctx, seeder) = Build();
        using (ctx)
        {
            await seeder.SeedAsync();
            var saudi = await ctx.Countries.FirstOrDefaultAsync(c => c.IsoAlpha3 == "SAU");
            saudi.Should().NotBeNull();
            saudi!.NameEn.Should().Be("Saudi Arabia");
        }
    }

    [Fact]
    public async Task Second_run_is_idempotent()
    {
        var (ctx, seeder) = Build();
        using (ctx)
        {
            await seeder.SeedAsync();
            var firstCountries = await ctx.Countries.CountAsync();
            await seeder.SeedAsync();
            var secondCountries = await ctx.Countries.CountAsync();
            secondCountries.Should().Be(firstCountries);
        }
    }

    [Fact]
    public async Task City_technologies_seeded()
    {
        var (ctx, seeder) = Build();
        using (ctx)
        {
            await seeder.SeedAsync();
            (await ctx.CityTechnologies.CountAsync()).Should().BeGreaterThan(0);
        }
    }

    [Fact]
    public async Task Notification_templates_seeded()
    {
        var (ctx, seeder) = Build();
        using (ctx)
        {
            await seeder.SeedAsync();
            var account = await ctx.NotificationTemplates.FirstOrDefaultAsync(t => t.Code == "ACCOUNT_CREATED");
            account.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task Static_pages_seeded()
    {
        var (ctx, seeder) = Build();
        using (ctx)
        {
            await seeder.SeedAsync();
            (await ctx.Pages.CountAsync()).Should().Be(3);
        }
    }

    [Fact]
    public async Task Homepage_sections_seeded()
    {
        var (ctx, seeder) = Build();
        using (ctx)
        {
            await seeder.SeedAsync();
            (await ctx.HomepageSections.CountAsync()).Should().BeGreaterThanOrEqualTo(5);
        }
    }
}
