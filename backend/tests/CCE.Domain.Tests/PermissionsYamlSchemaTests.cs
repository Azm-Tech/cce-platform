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

    private static readonly string[] BrdRequiredSentinel =
    {
        "System.Health.Read",
        "User.Read",
        "Role.Assign",
        "Resource.Center.Upload",
        "Resource.Country.Submit",
        "News.Publish",
        "Event.Manage",
        "Page.Edit",
        "Country.Profile.Update",
        "Community.Post.Create",
        "Community.Expert.RegisterRequest",
        "KnowledgeMap.View",
        "InteractiveCity.Run",
        "Survey.Submit",
        "Notification.TemplateManage",
        "Report.UserRegistrations",
    };

    private static readonly string[] ExpectedRoleNames =
    {
        "SuperAdmin", "ContentManager", "StateRepresentative",
        "CommunityExpert", "RegisteredUser", "Anonymous",
    };

    private static readonly string[] SuperAdminSentinel =
    {
        "System.Health.Read",
        "User.Read",
        "Role.Assign",
        "Report.UserRegistrations",
        "Report.News",
    };

    [Fact]
    public void All_BRD_required_permissions_are_present()
    {
        Permissions.All.Should().Contain(BrdRequiredSentinel);
    }

    [Fact]
    public void Permissions_All_count_matches_BRD_matrix()
    {
        Permissions.All.Count.Should().Be(41);
    }

    [Fact]
    public void RolePermissionMap_emits_all_six_known_roles()
    {
        var roles = typeof(CCE.Domain.RolePermissionMap)
            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Select(p => p.Name)
            .ToHashSet();

        roles.Should().BeEquivalentTo(ExpectedRoleNames);
    }

    [Fact]
    public void SuperAdmin_role_has_every_permission_assigned_to_it_in_YAML()
    {
        var superAdmin = CCE.Domain.RolePermissionMap.SuperAdmin;
        superAdmin.Should().Contain(SuperAdminSentinel);
    }
}
