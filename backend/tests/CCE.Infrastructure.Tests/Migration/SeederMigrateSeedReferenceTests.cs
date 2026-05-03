using CCE.Application;
using CCE.Domain.Identity;
using CCE.Domain.KnowledgeMaps;
using CCE.Infrastructure;
using CCE.Infrastructure.Persistence;
using CCE.Seeder;
using CCE.Seeder.Seeders;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CCE.Infrastructure.Tests.Migration;

[Collection(nameof(MigratorCollection))]
public sealed class SeederMigrateSeedReferenceTests
{
    private readonly MigratorFixture _fixture;

    public SeederMigrateSeedReferenceTests(MigratorFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task RunAllAsync_IsIdempotent_OnSecondRun()
    {
        // Arrange — fresh migrated DB.
        await using var ctxSetup = _fixture.CreateContextWithFreshDb("seedref_idempotent");
        await ctxSetup.Database.EnsureDeletedAsync();
        await ctxSetup.Database.MigrateAsync();

        var connectionString = _fixture.BuildConnectionString("seedref_idempotent");

        // Build a service provider matching the migrator container's DI graph.
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Infrastructure:SqlConnectionString"] = connectionString,
                ["Infrastructure:RedisConnectionString"] = "localhost:6379", // unused in seeders
                ["ConnectionStrings:Default"] = connectionString,
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddInfrastructure(configuration);
        services.AddScoped<ISeeder, ReferenceDataSeeder>();
        services.AddScoped<ISeeder, RolesAndPermissionsSeeder>();
        services.AddScoped<ISeeder, KnowledgeMapSeeder>();
        services.AddScoped<ISeeder, DemoDataSeeder>();
        services.AddScoped<SeedRunner>();
        await using var sp = services.BuildServiceProvider();

        // Act — run once, count rows; run again, count rows.
        var first = await RunAndCount(sp);
        var second = await RunAndCount(sp);

        // Assert — counts are non-zero (seeders did something) AND identical (idempotent).
        first.Roles.Should().BeGreaterThan(0);
        first.KnowledgeMaps.Should().BeGreaterThan(0);
        first.KnowledgeNodes.Should().BeGreaterThan(0);
        second.Should().BeEquivalentTo(first, "second run must not duplicate any rows");
    }

    private static async Task<(int Roles, int KnowledgeMaps, int KnowledgeNodes)> RunAndCount(IServiceProvider sp)
    {
        await using var scope = sp.CreateAsyncScope();
        var runner = scope.ServiceProvider.GetRequiredService<SeedRunner>();
        await runner.RunAllAsync(includeDemo: false).ConfigureAwait(false);
        var ctx = scope.ServiceProvider.GetRequiredService<CceDbContext>();
        return (
            Roles: await ctx.Set<Role>().CountAsync().ConfigureAwait(false),
            KnowledgeMaps: await ctx.Set<KnowledgeMap>().CountAsync().ConfigureAwait(false),
            KnowledgeNodes: await ctx.Set<KnowledgeMapNode>().CountAsync().ConfigureAwait(false));
    }
}
