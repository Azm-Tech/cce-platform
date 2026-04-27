using CCE.Domain.Identity;

namespace CCE.Domain.Tests.Identity;

public class RoleTests
{
    [Fact]
    public void Role_inherits_IdentityRole_of_Guid()
    {
        var baseType = typeof(Role).BaseType!;
        baseType.Name.Should().Be("IdentityRole`1");
        baseType.GetGenericArguments()[0].Should().Be(typeof(System.Guid));
    }

    [Fact]
    public void Role_constructed_with_name_sets_Name()
    {
        var role = new Role("SuperAdmin");
        role.Name.Should().Be("SuperAdmin");
    }

    [Fact]
    public void Role_default_constructor_leaves_name_null()
    {
        var role = new Role();
        role.Name.Should().BeNull();
    }
}
