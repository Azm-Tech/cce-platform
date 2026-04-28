using System.Reflection;
using CCE.Domain.Common;

namespace CCE.ArchitectureTests;

public class AuditPolicyTests
{
    private static readonly Assembly DomainAssembly = typeof(Entity<>).Assembly;

    [Fact]
    public void Every_aggregate_root_has_Audited_attribute()
    {
        var aggregates = DomainAssembly.GetTypes()
            .Where(t => !t.IsAbstract
                        && t.IsClass
                        && IsSubclassOfRawGeneric(typeof(AggregateRoot<>), t))
            .ToList();

        var unaudited = aggregates
            .Where(t => t.GetCustomAttribute<AuditedAttribute>(inherit: false) is null)
            .ToList();

        unaudited.Should().BeEmpty(
            because: $"all aggregate roots must be marked [Audited] (spec §4.11). Missing: {string.Join(", ", unaudited.Select(t => t.Name))}");
    }

    private static bool IsSubclassOfRawGeneric(System.Type generic, System.Type? toCheck)
    {
        while (toCheck is not null && toCheck != typeof(object))
        {
            var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
            if (generic == cur) return true;
            toCheck = toCheck.BaseType;
        }
        return false;
    }
}
