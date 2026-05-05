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
                  roles: [cce-admin]
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
                  roles: [cce-admin]
            """;

        var generated = GeneratorTestHarness.Run(yaml);

        // Sub-11 Phase 03: role values like "cce-admin" become PascalCase
        // C# identifiers ("CceAdmin") via PermissionsGenerator.ToRoleMemberName.
        generated.Should().Contain("public static IReadOnlyList<string> CceAdmin { get; } = new[]");
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
                  roles: [cce-admin, cce-editor]
            """;

        var generated = GeneratorTestHarness.Run(yaml);

        var cceAdminBlock = ExtractRoleBlock(generated, "CceAdmin");
        var cceEditorBlock = ExtractRoleBlock(generated, "CceEditor");

        cceAdminBlock.Should().Contain("\"Page.Edit\"");
        cceEditorBlock.Should().Contain("\"Page.Edit\"");
    }

    [Fact]
    public void All_six_roles_are_emitted_even_when_some_have_no_permissions()
    {
        const string yaml = """
            groups:
              User:
                Delete:
                  description: x
                  roles: [cce-admin]
            """;

        var generated = GeneratorTestHarness.Run(yaml);

        // Sub-11 Phase 03 Entra ID app-role values, PascalCased for the
        // generated C# property names.
        generated.Should().Contain("public static IReadOnlyList<string> CceAdmin { get; }");
        generated.Should().Contain("public static IReadOnlyList<string> CceEditor { get; }");
        generated.Should().Contain("public static IReadOnlyList<string> CceReviewer { get; }");
        generated.Should().Contain("public static IReadOnlyList<string> CceExpert { get; }");
        generated.Should().Contain("public static IReadOnlyList<string> CceUser { get; }");
        generated.Should().Contain("public static IReadOnlyList<string> Anonymous { get; }");
    }

    /// <summary>
    /// Returns the substring of <paramref name="generated"/> covering the body of the named role's
    /// collection, e.g. for role "CceAdmin": everything between
    /// "public static IReadOnlyList&lt;string&gt; CceAdmin { get; } = new[]" and the next "};".
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
