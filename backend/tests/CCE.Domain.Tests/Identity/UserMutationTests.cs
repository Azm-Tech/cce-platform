using CCE.Domain.Common;
using CCE.Domain.Identity;

namespace CCE.Domain.Tests.Identity;

public class UserMutationTests
{
    [Fact]
    public void SetLocalePreference_accepts_ar()
    {
        var user = new User();
        user.SetLocalePreference("ar");
        user.LocalePreference.Should().Be("ar");
    }

    [Fact]
    public void SetLocalePreference_accepts_en()
    {
        var user = new User();
        user.SetLocalePreference("en");
        user.LocalePreference.Should().Be("en");
    }

    [Theory]
    [InlineData("fr")]
    [InlineData("AR")]
    [InlineData("")]
    [InlineData("  ")]
    public void SetLocalePreference_rejects_anything_else(string invalid)
    {
        var user = new User();
        var act = () => user.SetLocalePreference(invalid);
        act.Should().Throw<DomainException>().WithMessage("*locale*");
    }

    [Fact]
    public void SetLocalePreference_rejects_null()
    {
        var user = new User();
        var act = () => user.SetLocalePreference(null!);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void SetKnowledgeLevel_updates_field()
    {
        var user = new User();
        user.SetKnowledgeLevel(KnowledgeLevel.Advanced);
        user.KnowledgeLevel.Should().Be(KnowledgeLevel.Advanced);
    }

    [Fact]
    public void UpdateInterests_replaces_list()
    {
        var user = new User();
        user.UpdateInterests(new[] { "Solar", "Wind" });
        user.Interests.Should().Equal("Solar", "Wind");
    }

    [Fact]
    public void UpdateInterests_with_null_throws()
    {
        var user = new User();
        var act = () => user.UpdateInterests(null!);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void UpdateInterests_deduplicates_and_trims()
    {
        var user = new User();
        user.UpdateInterests(new[] { " Solar ", "Solar", "Wind", "" });
        user.Interests.Should().Equal("Solar", "Wind");
    }

    [Fact]
    public void AssignCountry_sets_id()
    {
        var user = new User();
        var country = System.Guid.NewGuid();
        user.AssignCountry(country);
        user.CountryId.Should().Be(country);
    }

    [Fact]
    public void ClearCountry_sets_null()
    {
        var user = new User { CountryId = System.Guid.NewGuid() };
        user.ClearCountry();
        user.CountryId.Should().BeNull();
    }

    [Fact]
    public void SetAvatarUrl_accepts_https_url()
    {
        var user = new User();
        user.SetAvatarUrl("https://cdn.example/avatar.png");
        user.AvatarUrl.Should().Be("https://cdn.example/avatar.png");
    }

    [Fact]
    public void SetAvatarUrl_rejects_non_https()
    {
        var user = new User();
        var act = () => user.SetAvatarUrl("http://insecure.example/x.png");
        act.Should().Throw<DomainException>().WithMessage("*https*");
    }

    [Fact]
    public void SetAvatarUrl_with_null_clears_value()
    {
        var user = new User();
        user.SetAvatarUrl("https://cdn.example/a.png");
        user.SetAvatarUrl(null);
        user.AvatarUrl.Should().BeNull();
    }
}
