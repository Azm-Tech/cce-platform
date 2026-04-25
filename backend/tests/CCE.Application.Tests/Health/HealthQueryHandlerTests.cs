using CCE.Application.Health;
using CCE.Domain.Common;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Health;

public class HealthQueryHandlerTests
{
    [Fact]
    public async Task Returns_ok_status_with_locale_and_now_timestamp()
    {
        var clock = new FakeSystemClock();
        var sut = new HealthQueryHandler(clock);
        var query = new HealthQuery(Locale: "ar");

        var result = await sut.Handle(query, CancellationToken.None);

        result.Status.Should().Be("ok");
        result.Locale.Should().Be("ar");
        result.UtcNow.Should().Be(FakeSystemClock.DefaultStart);
        result.Version.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Defaults_locale_to_ar_when_unspecified()
    {
        var sut = new HealthQueryHandler(new FakeSystemClock());

        var result = await sut.Handle(new HealthQuery(Locale: null), CancellationToken.None);

        result.Locale.Should().Be("ar");
    }

    [Fact]
    public async Task Echoes_explicit_en_locale()
    {
        var sut = new HealthQueryHandler(new FakeSystemClock());

        var result = await sut.Handle(new HealthQuery(Locale: "en"), CancellationToken.None);

        result.Locale.Should().Be("en");
    }
}
