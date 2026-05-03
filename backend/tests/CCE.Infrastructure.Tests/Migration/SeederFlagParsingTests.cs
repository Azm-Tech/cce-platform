using CCE.Seeder;
using FluentAssertions;
using Xunit;

namespace CCE.Infrastructure.Tests.Migration;

public sealed class SeederFlagParsingTests
{
    [Fact]
    public void NoFlags_ReturnsRunSeeders()
    {
        SeederMode.Parse(Array.Empty<string>())
            .Should().BeEquivalentTo(new SeederMode(SeederMode.Kind.RunSeeders, null));
    }

    [Fact]
    public void DemoFlag_ReturnsRunSeedersWithDemo()
    {
        SeederMode.Parse(new[] { "--demo" })
            .Should().BeEquivalentTo(new SeederMode(SeederMode.Kind.RunSeedersWithDemo, null));
    }

    [Fact]
    public void MigrateFlag_ReturnsMigrateOnly()
    {
        SeederMode.Parse(new[] { "--migrate" })
            .Should().BeEquivalentTo(new SeederMode(SeederMode.Kind.MigrateOnly, null));
    }

    [Fact]
    public void MigrateAndSeedReference_ReturnsMigrateAndSeedReference()
    {
        SeederMode.Parse(new[] { "--migrate", "--seed-reference" })
            .Should().BeEquivalentTo(new SeederMode(SeederMode.Kind.MigrateAndSeedReference, null));
    }

    [Fact]
    public void MigrateAndDemo_ReturnsErrorWithMessage()
    {
        var mode = SeederMode.Parse(new[] { "--migrate", "--demo" });
        mode.Mode.Should().Be(SeederMode.Kind.Error);
        mode.ErrorMessage.Should().Contain("Demo data is not allowed in migration mode");
    }

    [Fact]
    public void SeedReferenceWithoutMigrate_ReturnsError()
    {
        var mode = SeederMode.Parse(new[] { "--seed-reference" });
        mode.Mode.Should().Be(SeederMode.Kind.Error);
        mode.ErrorMessage.Should().Contain("--seed-reference requires --migrate");
    }

    [Fact]
    public void FlagOrder_DoesNotMatter()
    {
        SeederMode.Parse(new[] { "--seed-reference", "--migrate" })
            .Mode.Should().Be(SeederMode.Kind.MigrateAndSeedReference);
    }

    [Fact]
    public void FlagCase_IsNotSensitive()
    {
        SeederMode.Parse(new[] { "--MIGRATE", "--Seed-Reference" })
            .Mode.Should().Be(SeederMode.Kind.MigrateAndSeedReference);
    }

    [Fact]
    public void UnknownFlags_AreIgnored()
    {
        // We don't strictly validate the full args set — extra flags are tolerated
        // (Host.CreateApplicationBuilder consumes its own).
        SeederMode.Parse(new[] { "--migrate", "--some-future-flag" })
            .Mode.Should().Be(SeederMode.Kind.MigrateOnly);
    }
}
