using System.Reflection;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CCE.ArchitectureTests;

public class InfrastructureTests
{
    private static readonly Assembly InfrastructureAssembly = typeof(CceDbContext).Assembly;

    [Fact]
    public void Configurations_are_sealed()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .That()
            .ImplementInterface(typeof(IEntityTypeConfiguration<>))
            .Should()
            .BeSealed()
            .GetResult();
        result.IsSuccessful.Should().BeTrue(BecauseFailing(result));
    }

    [Fact]
    public void Configurations_reside_in_Configurations_namespace()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .That()
            .ImplementInterface(typeof(IEntityTypeConfiguration<>))
            .Should()
            .ResideInNamespaceStartingWith("CCE.Infrastructure.Persistence.Configurations")
            .GetResult();
        result.IsSuccessful.Should().BeTrue(BecauseFailing(result));
    }

    private static string BecauseFailing(TestResult r) =>
        r.IsSuccessful ? "ok" :
            "failing types: " + string.Join(", ", r.FailingTypes?.Select(t => t.FullName) ?? new[] { "none" });
}
