using CCE.Domain.Common;
using CCE.Domain.Notifications;

namespace CCE.Domain.Tests.Notifications;

public class NotificationTemplateTests
{
    private static NotificationTemplate NewTemplate() => NotificationTemplate.Define(
        "ACCOUNT_CREATED", "تم إنشاء حسابك", "Your account is created",
        "مرحباً", "Welcome", NotificationChannel.Email, "{}");

    [Fact]
    public void Define_creates_active_template() {
        var t = NewTemplate();
        t.IsActive.Should().BeTrue();
        t.Channel.Should().Be(NotificationChannel.Email);
    }

    [Theory]
    [InlineData("lowercase")]
    [InlineData("Mixed_Case")]
    [InlineData("HAS-DASH")]
    [InlineData("123_LEADING_DIGIT")]
    public void Code_must_be_upper_snake_case(string bad) {
        var act = () => NotificationTemplate.Define(
            bad, "ا", "x", "ا", "x", NotificationChannel.Email, "{}");
        act.Should().Throw<DomainException>().WithMessage("*Code*");
    }

    [Fact]
    public void UpdateContent_replaces_subject_body() {
        var t = NewTemplate();
        t.UpdateContent("ج", "new subject", "ج", "new body");
        t.SubjectEn.Should().Be("new subject");
        t.BodyAr.Should().Be("ج");
    }

    [Fact]
    public void Deactivate_then_Activate_toggles() {
        var t = NewTemplate();
        t.Deactivate();
        t.IsActive.Should().BeFalse();
        t.Activate();
        t.IsActive.Should().BeTrue();
    }
}
