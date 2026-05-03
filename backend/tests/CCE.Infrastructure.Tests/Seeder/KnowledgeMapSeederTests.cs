using CCE.Infrastructure.Persistence;
using CCE.Seeder.Seeders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CCE.Infrastructure.Tests.Seeder;

public class KnowledgeMapSeederTests
{
    // Numbers reflect the Sub-7 ship-readiness enrichment of the seeder
    // (cce-basics + carbon-capture maps; 13 + 9 = 22 nodes, 17 + 9 = 26 edges).
    private const int ExpectedMaps = 2;
    private const int ExpectedNodes = 22;
    private const int ExpectedEdges = 26;

    private static (CceDbContext Ctx, KnowledgeMapSeeder Seeder) Build()
    {
        var ctx = new CceDbContext(new DbContextOptionsBuilder<CceDbContext>()
            .UseInMemoryDatabase(System.Guid.NewGuid().ToString())
            .Options);
        return (ctx, new KnowledgeMapSeeder(ctx, NullLogger<KnowledgeMapSeeder>.Instance));
    }

    [Fact]
    public async Task Seeds_two_maps_with_full_node_and_edge_complement()
    {
        var (ctx, seeder) = Build();
        using (ctx)
        {
            await seeder.SeedAsync();
            (await ctx.KnowledgeMaps.CountAsync()).Should().Be(ExpectedMaps);
            (await ctx.KnowledgeMapNodes.CountAsync()).Should().Be(ExpectedNodes);
            (await ctx.KnowledgeMapEdges.CountAsync()).Should().Be(ExpectedEdges);
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
            (await ctx.KnowledgeMapNodes.CountAsync()).Should().Be(ExpectedNodes);
            (await ctx.KnowledgeMapEdges.CountAsync()).Should().Be(ExpectedEdges);
        }
    }
}
