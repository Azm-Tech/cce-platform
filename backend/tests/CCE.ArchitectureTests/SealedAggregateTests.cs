using System.Reflection;
using CCE.Domain.Common;

namespace CCE.ArchitectureTests;

public class SealedAggregateTests
{
    private static readonly Assembly DomainAssembly = typeof(Entity<>).Assembly;

    [Fact]
    public void All_concrete_entities_are_sealed_or_extend_Identity()
    {
        var allTypes = DomainAssembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface
                        && t.IsClass
                        && (t.IsSubclassOf(typeof(Entity<System.Guid>))
                            || (t.BaseType?.Name?.StartsWith("Identity", System.StringComparison.Ordinal) ?? false)))
            .ToList();

        var nonSealed = allTypes.Where(t => !t.IsSealed
            && !(t.BaseType?.Name?.StartsWith("Identity", System.StringComparison.Ordinal) ?? false))
            .ToList();

        nonSealed.Should().BeEmpty(
            because: "concrete domain entities must be sealed; only Identity extension classes (User, Role) are exempt");
    }
}
