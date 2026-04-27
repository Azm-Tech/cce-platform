namespace CCE.Domain.SourceGenerators.Tests;

public class RolePermissionMapGeneratorTests
{
    [Fact]
    public void RolePermissionMap_class_is_emitted()
    {
        const string yaml = """
            groups:
              User:
                Read:
                  description: x
                  roles: [SuperAdmin]
            """;

        var generated = GeneratorTestHarness.Run(yaml);

        generated.Should().Contain("public static class RolePermissionMap");
    }

    [Fact]
    public void Role_with_one_permission_emits_single_entry_collection()
    {
        const string yaml = """
            groups:
              User:
                Read:
                  description: x
                  roles: [SuperAdmin]
            """;

        var generated = GeneratorTestHarness.Run(yaml);

        generated.Should().Contain("public static IReadOnlyList<string> SuperAdmin { get; } = new[]");
        generated.Should().Contain("\"User.Read\",");
    }

    [Fact]
    public void Permission_assigned_to_multiple_roles_appears_in_each_role_collection()
    {
        const string yaml = """
            groups:
              Page:
                Edit:
                  description: x
                  roles: [SuperAdmin, ContentManager]
            """;

        var generated = GeneratorTestHarness.Run(yaml);

        var superAdminBlock = ExtractRoleBlock(generated, "SuperAdmin");
        var contentManagerBlock = ExtractRoleBlock(generated, "ContentManager");

        superAdminBlock.Should().Contain("\"Page.Edit\"");
        contentManagerBlock.Should().Contain("\"Page.Edit\"");
    }

    [Fact]
    public void All_six_roles_are_emitted_even_when_some_have_no_permissions()
    {
        const string yaml = """
            groups:
              User:
                Delete:
                  description: x
                  roles: [SuperAdmin]
            """;

        var generated = GeneratorTestHarness.Run(yaml);

        generated.Should().Contain("public static IReadOnlyList<string> SuperAdmin { get; }");
        generated.Should().Contain("public static IReadOnlyList<string> ContentManager { get; }");
        generated.Should().Contain("public static IReadOnlyList<string> StateRepresentative { get; }");
        generated.Should().Contain("public static IReadOnlyList<string> CommunityExpert { get; }");
        generated.Should().Contain("public static IReadOnlyList<string> RegisteredUser { get; }");
        generated.Should().Contain("public static IReadOnlyList<string> Anonymous { get; }");
    }

    /// <summary>
    /// Returns the substring of <paramref name="generated"/> covering the body of the named role's
    /// collection, e.g. for role "SuperAdmin": everything between
    /// "public static IReadOnlyList&lt;string&gt; SuperAdmin { get; } = new[]" and the next "};".
    /// </summary>
    private static string ExtractRoleBlock(string generated, string roleName)
    {
        var marker = $"IReadOnlyList<string> {roleName} {{ get; }} = new[]";
        var start = generated.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0)
        {
            return string.Empty;
        }
        var end = generated.IndexOf("};", start, StringComparison.Ordinal);
        return end < 0 ? generated.Substring(start) : generated.Substring(start, end - start);
    }
}
