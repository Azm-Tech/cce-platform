using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CCE.Infrastructure.Tests.Migration;

[Collection(nameof(MigratorCollection))]
public sealed class SeederMigrateModeTests
{
    private readonly MigratorFixture _fixture;

    public SeederMigrateModeTests(MigratorFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task MigrateAsync_AppliesAllPendingMigrations_ToFreshDatabase()
    {
        // Arrange — fresh DB.
        await using var ctx = _fixture.CreateContextWithFreshDb("migrate_fresh");
        await ctx.Database.EnsureDeletedAsync();

        // Act
        var pending = (await ctx.Database.GetPendingMigrationsAsync()).ToList();
        await ctx.Database.MigrateAsync();

        // Assert: every existing migration is now applied.
        var applied = (await ctx.Database.GetAppliedMigrationsAsync()).ToList();
        pending.Should().NotBeEmpty("the repo has at least three EF migrations");
        applied.Should().Contain(pending);
    }

    [Fact]
    public async Task MigrateAsync_IsNoOp_OnAlreadyMigratedDatabase()
    {
        // Arrange — migrate once.
        await using var ctx = _fixture.CreateContextWithFreshDb("migrate_idempotent");
        await ctx.Database.EnsureDeletedAsync();
        await ctx.Database.MigrateAsync();
        var firstApplied = (await ctx.Database.GetAppliedMigrationsAsync()).Count();

        // Act — migrate again on the same DB.
        await using var ctx2 = _fixture.CreateContextWithFreshDb("migrate_idempotent");
        var pendingBefore = (await ctx2.Database.GetPendingMigrationsAsync()).ToList();
        await ctx2.Database.MigrateAsync();
        var secondApplied = (await ctx2.Database.GetAppliedMigrationsAsync()).Count();

        // Assert: second run had nothing pending and didn't change applied count.
        pendingBefore.Should().BeEmpty();
        secondApplied.Should().Be(firstApplied);
    }
}
