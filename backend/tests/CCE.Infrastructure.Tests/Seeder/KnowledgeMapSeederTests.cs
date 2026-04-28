using CCE.Infrastructure.Persistence;
using CCE.Seeder.Seeders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CCE.Infrastructure.Tests.Seeder;

public class KnowledgeMapSeederTests
{
    private static (CceDbContext Ctx, KnowledgeMapSeeder Seeder) Build()
    {
        var ctx = new CceDbContext(new DbContextOptionsBuilder<CceDbContext>()
            .UseInMemoryDatabase(System.Guid.NewGuid().ToString())
            .Options);
        return (ctx, new KnowledgeMapSeeder(ctx, NullLogger<KnowledgeMapSeeder>.Instance));
    }

    [Fact]
    public async Task Seeds_one_map_with_4_nodes_and_3_edges()
    {
        var (ctx, seeder) = Build();
        using (ctx)
        {
            await seeder.SeedAsync();
            (await ctx.KnowledgeMaps.CountAsync()).Should().Be(1);
            (await ctx.KnowledgeMapNodes.CountAsync()).Should().Be(4);
            (await ctx.KnowledgeMapEdges.CountAsync()).Should().Be(3);
        }
    }

    [Fact]
    public async Task Re_running_does_not_duplicate()
    {
        var (ctx, seeder) = Build();
        using (ctx)
        {
            await seeder.SeedAsync();
            await seeder.SeedAsync();
            (await ctx.KnowledgeMapNodes.CountAsync()).Should().Be(4);
            (await ctx.KnowledgeMapEdges.CountAsync()).Should().Be(3);
        }
    }
}
