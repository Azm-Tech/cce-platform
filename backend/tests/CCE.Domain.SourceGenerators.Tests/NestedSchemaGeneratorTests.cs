namespace CCE.Domain.SourceGenerators.Tests;

public class NestedSchemaGeneratorTests
{
    [Fact]
    public void Two_level_nested_yields_dotted_constant()
    {
        const string yaml = """
            groups:
              User:
                Read:
                  description: Read user profiles
                  roles: [SuperAdmin]
            """;

        var generated = GeneratorTestHarness.Run(yaml);

        generated.Should().Contain("public const string User_Read = \"User.Read\";");
        generated.Should().Contain("User_Read,");
    }

    [Fact]
    public void Three_level_nested_yields_three_segment_constant()
    {
        const string yaml = """
            groups:
              Resource:
                Center:
                  Upload:
                    description: Upload a center-managed resource
                    roles: [SuperAdmin, ContentManager]
            """;

        var generated = GeneratorTestHarness.Run(yaml);

        generated.Should().Contain("public const string Resource_Center_Upload = \"Resource.Center.Upload\";");
    }

    [Fact]
    public void Multiple_top_level_groups_each_emit_their_permissions()
    {
        const string yaml = """
            groups:
              User:
                Read:
                  description: Read users
                  roles: [SuperAdmin]
                Create:
                  description: Create users
                  roles: [SuperAdmin]
              Page:
                Edit:
                  description: Edit pages
                  roles: [SuperAdmin, ContentManager]
            """;

        var generated = GeneratorTestHarness.Run(yaml);

        generated.Should().Contain("User_Read = \"User.Read\"");
        generated.Should().Contain("User_Create = \"User.Create\"");
        generated.Should().Contain("Page_Edit = \"Page.Edit\"");
    }

    [Fact]
    public void Comments_and_blank_lines_are_ignored()
    {
        const string yaml = """
            # Header comment
            groups:
              # mid comment
              User:

                Read:
                  description: x
                  roles: [SuperAdmin]
            """;

        var generated = GeneratorTestHarness.Run(yaml);

        generated.Should().Contain("User_Read = \"User.Read\"");
    }
}
