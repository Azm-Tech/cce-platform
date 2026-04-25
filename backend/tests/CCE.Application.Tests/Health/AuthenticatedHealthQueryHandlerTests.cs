using CCE.Application.Health;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Health;

public class AuthenticatedHealthQueryHandlerTests
{
    [Fact]
    public async Task Echoes_user_id_and_groups_in_result()
    {
        var clock = new FakeSystemClock();
        var sut = new AuthenticatedHealthQueryHandler(clock);
        var query = new AuthenticatedHealthQuery(
            UserId: "test-user-id",
            PreferredUsername: "admin@cce.local",
            Email: "admin@cce.local",
            Upn: "admin@cce.local",
            Groups: ["SuperAdmin", "default-roles-cce-internal"],
            Locale: "en");

        var result = await sut.Handle(query, CancellationToken.None);

        result.Status.Should().Be("ok");
        result.User.Id.Should().Be("test-user-id");
        result.User.PreferredUsername.Should().Be("admin@cce.local");
        result.User.Upn.Should().Be("admin@cce.local");
        result.User.Groups.Should().Contain("SuperAdmin");
        result.Locale.Should().Be("en");
        result.UtcNow.Should().Be(FakeSystemClock.DefaultStart);
    }

    [Fact]
    public async Task Defaults_locale_to_ar_when_unspecified()
    {
        var sut = new AuthenticatedHealthQueryHandler(new FakeSystemClock());
        var query = new AuthenticatedHealthQuery(
            UserId: "x", PreferredUsername: "x", Email: "x", Upn: "x",
            Groups: [], Locale: null);

        var result = await sut.Handle(query, CancellationToken.None);

        result.Locale.Should().Be("ar");
    }
}
