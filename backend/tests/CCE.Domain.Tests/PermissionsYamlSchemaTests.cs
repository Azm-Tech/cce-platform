using CCE.Domain;

namespace CCE.Domain.Tests;

public class PermissionsYamlSchemaTests
{
    [Fact]
    public void Foundation_seed_System_Health_Read_remains_present()
    {
        Permissions.System_Health_Read.Should().Be("System.Health.Read");
        Permissions.All.Should().Contain("System.Health.Read");
    }

    [Fact]
    public void Permissions_All_is_non_empty()
    {
        Permissions.All.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Every_All_entry_uses_dot_notation()
    {
        foreach (var permission in Permissions.All)
        {
            permission.Should().MatchRegex(@"^[A-Z][A-Za-z0-9]+(\.[A-Z][A-Za-z0-9]+)+$",
                because: $"permission '{permission}' should be PascalCase dot-notation");
        }
    }
}
