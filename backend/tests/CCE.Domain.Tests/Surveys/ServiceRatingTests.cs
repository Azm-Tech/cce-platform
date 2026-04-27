using CCE.Domain.Common;
using CCE.Domain.Surveys;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Surveys;

public class ServiceRatingTests
{
    [Fact]
    public void Submit_anonymous_rating() {
        var r = ServiceRating.Submit(null, 4, null, null, "/home", "ar", new FakeSystemClock());
        r.UserId.Should().BeNull();
        r.Rating.Should().Be(4);
    }

    [Fact]
    public void Submit_with_user_and_comment() {
        var clock = new FakeSystemClock();
        var user = System.Guid.NewGuid();
        var r = ServiceRating.Submit(user, 5, "ممتاز", "Excellent", "/about", "en", clock);
        r.UserId.Should().Be(user);
        r.CommentEn.Should().Be("Excellent");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Out_of_range_rating_throws(int bad) {
        var act = () => ServiceRating.Submit(null, bad, null, null, "/x", "ar", new FakeSystemClock());
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Empty_page_throws() {
        var act = () => ServiceRating.Submit(null, 3, null, null, "", "ar", new FakeSystemClock());
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Invalid_locale_throws() {
        var act = () => ServiceRating.Submit(null, 3, null, null, "/x", "fr", new FakeSystemClock());
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ServiceRating_is_NOT_audited() {
        typeof(ServiceRating).GetCustomAttributes(typeof(AuditedAttribute), inherit: false)
            .Should().BeEmpty();
    }
}
