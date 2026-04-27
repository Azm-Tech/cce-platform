namespace CCE.Domain.SourceGenerators.Tests;

public class SmokeGeneratorTests
{
    [Fact]
    public void Flat_schema_with_one_permission_emits_constant_and_All_collection()
    {
        const string yaml = """
            permissions:
              - System.Health.Read
            """;

        var generated = GeneratorTestHarness.Run(yaml);

        generated.Should().Contain("public const string System_Health_Read = \"System.Health.Read\";");
        generated.Should().Contain("public static IReadOnlyList<string> All { get; }");
        generated.Should().Contain("System_Health_Read,");
    }

    [Fact]
    public void Empty_yaml_still_emits_a_compilable_Permissions_class()
    {
        var generated = GeneratorTestHarness.Run(string.Empty);

        generated.Should().Contain("public static class Permissions");
        generated.Should().Contain("public static IReadOnlyList<string> All { get; }");
    }
}
