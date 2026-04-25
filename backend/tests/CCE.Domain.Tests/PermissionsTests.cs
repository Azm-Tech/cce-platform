using CCE.Domain;

namespace CCE.Domain.Tests;

public class PermissionsTests
{
    [Fact]
    public void System_Health_Read_constant_matches_YAML_value()
    {
        Permissions.System_Health_Read.Should().Be("System.Health.Read");
    }

    [Fact]
    public void All_collection_contains_System_Health_Read()
    {
        Permissions.All.Should().Contain("System.Health.Read");
    }

    [Fact]
    public void All_collection_is_not_empty()
    {
        Permissions.All.Should().NotBeEmpty();
    }
}
