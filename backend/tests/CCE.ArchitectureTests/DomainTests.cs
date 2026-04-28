using System.Reflection;
using CCE.Domain.Common;

namespace CCE.ArchitectureTests;

public class DomainTests
{
    private static readonly Assembly DomainAssembly = typeof(Entity<>).Assembly;

    [Fact]
    public void All_aggregate_roots_are_sealed()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .Inherit(typeof(AggregateRoot<>))
            .Should()
            .BeSealed()
            .GetResult();
        result.IsSuccessful.Should().BeTrue(BecauseFailing(result));
    }

    [Fact]
    public void Domain_events_are_sealed()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .ImplementInterface(typeof(IDomainEvent))
            .Should()
            .BeSealed()
            .GetResult();
        result.IsSuccessful.Should().BeTrue(BecauseFailing(result));
    }

    [Fact]
    public void All_entities_live_under_CCE_Domain_namespace()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .Inherit(typeof(Entity<>))
            .Should()
            .ResideInNamespaceStartingWith("CCE.Domain")
            .GetResult();
        result.IsSuccessful.Should().BeTrue(BecauseFailing(result));
    }

    private static string BecauseFailing(TestResult r) =>
        r.IsSuccessful ? "ok" :
            "failing types: " + string.Join(", ", r.FailingTypes?.Select(t => t.FullName) ?? new[] { "none" });
}
