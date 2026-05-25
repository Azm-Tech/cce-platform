using CCE.Domain.Identity;

namespace CCE.Domain.Tests.Identity;

public class UserDefaultsTests
{
    [Fact]
    public void New_user_defaults_LocalePreference_to_ar()
    {
        var user = new User();
        user.LocalePreference.Should().Be("ar");
    }

    [Fact]
    public void New_user_defaults_KnowledgeLevel_to_Beginner()
    {
        var user = new User();
        user.KnowledgeLevel.Should().Be(KnowledgeLevel.Beginner);
    }

    [Fact]
    public void New_user_defaults_Interests_to_empty_list()
    {
        var user = new User();
        user.Interests.Should().BeEmpty();
    }

    [Fact]
    public void New_user_defaults_CountryId_to_null()
    {
        var user = new User();
        user.CountryId.Should().BeNull();
    }

    [Fact]
    public void New_user_defaults_AvatarUrl_to_null()
    {
        var user = new User();
        user.AvatarUrl.Should().BeNull();
    }

    [Fact]
    public void User_inherits_IdentityUser_of_Guid()
    {
        var baseType = typeof(User).BaseType!;
        baseType.Name.Should().Be("IdentityUser`1");
        baseType.GetGenericArguments()[0].Should().Be<System.Guid>();
    }
}
