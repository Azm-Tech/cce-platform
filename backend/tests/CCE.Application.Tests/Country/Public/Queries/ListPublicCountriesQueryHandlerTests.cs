using CCE.Application.Common.Interfaces;
using CCE.Application.CountryPublic.Queries.ListPublicCountries;
using CCE.Application.Localization;
using CCE.Application.Messages;
using CCE.Domain.Country;
using NSubstitute;

namespace CCE.Application.Tests.Country.Public.Queries;

public class ListPublicCountriesQueryHandlerTests
{
    private static MessageFactory BuildMessages()
    {
        var localization = Substitute.For<ILocalizationService>();
        localization.GetString(Arg.Any<string>(), Arg.Any<string?>()).Returns(call => call.ArgAt<string>(0));
        return new MessageFactory(localization, Microsoft.Extensions.Logging.Abstractions.NullLogger<MessageFactory>.Instance);
    }

    [Fact]
    public async Task Returns_empty_paged_result_when_no_countries_exist()
    {
        var db = BuildDb(
            System.Array.Empty<CCE.Domain.Country.Country>(),
            System.Array.Empty<CountryKapsarcSnapshot>());
        var sut = new ListPublicCountriesQueryHandler(db, BuildMessages());

        var result = await sut.Handle(new ListPublicCountriesQuery(), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().BeEmpty();
        result.Data.Total.Should().Be(0);
        result.Data.Page.Should().Be(1);
        result.Data.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task Returns_only_active_countries()
    {
        var active = CCE.Domain.Country.Country.Register("USA", "US", "أمريكا", "United States", "أمريكا الشمالية", "North America", "https://example/flag.png");
        var inactive = CCE.Domain.Country.Country.Register("GBR", "GB", "بريطانيا", "United Kingdom", "أوروبا", "Europe", "https://example/flag-gb.png");
        inactive.Deactivate();

        var db = BuildDb(new[] { active, inactive }, System.Array.Empty<CountryKapsarcSnapshot>());
        var sut = new ListPublicCountriesQueryHandler(db, BuildMessages());

        var result = await sut.Handle(new ListPublicCountriesQuery(), CancellationToken.None);

        result.Data!.Items.Should().HaveCount(1);
        result.Data.Items.Single().NameEn.Should().Be("United States");
    }

    [Fact]
    public async Task Search_filter_returns_countries_matching_NameEn_substring()
    {
        var usa = CCE.Domain.Country.Country.Register("USA", "US", "أمريكا", "United States", "أمريكا الشمالية", "North America", "https://example/flag.png");
        var gbr = CCE.Domain.Country.Country.Register("GBR", "GB", "بريطانيا", "United Kingdom", "أوروبا", "Europe", "https://example/flag-gb.png");

        var db = BuildDb(new[] { usa, gbr }, System.Array.Empty<CountryKapsarcSnapshot>());
        var sut = new ListPublicCountriesQueryHandler(db, BuildMessages());

        var result = await sut.Handle(new ListPublicCountriesQuery(Search: "United States"), CancellationToken.None);

        result.Data!.Items.Should().HaveCount(1);
        result.Data.Items.Single().NameEn.Should().Be("United States");
    }

    [Fact]
    public async Task Defaults_to_sort_by_PerformanceScore_descending()
    {
        var usa = CCE.Domain.Country.Country.Register("USA", "US", "أمريكا", "United States", "أمريكا الشمالية", "North America", "https://example/flag.png");
        var gbr = CCE.Domain.Country.Country.Register("GBR", "GB", "بريطانيا", "United Kingdom", "أوروبا", "Europe", "https://example/flag-gb.png");
        var saudi = CCE.Domain.Country.Country.Register("SAU", "SA", "المملكة العربية السعودية", "Saudi Arabia", "آسيا", "Asia", "https://example/flag-sa.png");

        var usaSnapshot = CountryKapsarcSnapshot.Capture(usa.Id, "Leader", 85.50m, 90.00m, new CCE.TestInfrastructure.Time.FakeSystemClock());
        var gbrSnapshot = CountryKapsarcSnapshot.Capture(gbr.Id, "Follower", 70.00m, 75.00m, new CCE.TestInfrastructure.Time.FakeSystemClock());
        usa.UpdateLatestKapsarcSnapshot(usaSnapshot.Id);
        gbr.UpdateLatestKapsarcSnapshot(gbrSnapshot.Id);

        var db = BuildDb(
            new[] { usa, gbr, saudi },
            new[] { usaSnapshot, gbrSnapshot });
        var sut = new ListPublicCountriesQueryHandler(db, BuildMessages());

        var result = await sut.Handle(new ListPublicCountriesQuery(), CancellationToken.None);

        result.Data!.Items.Should().HaveCount(3);
        result.Data.Items[0].CcePerformanceScore.Should().Be(85.50m);
        result.Data.Items[1].CcePerformanceScore.Should().Be(70.00m);
        result.Data.Items[2].CcePerformanceScore.Should().BeNull();
    }

    private static ICceDbContext BuildDb(
        IEnumerable<CCE.Domain.Country.Country> countries,
        IEnumerable<CountryKapsarcSnapshot> snapshots)
    {
        var db = Substitute.For<ICceDbContext>();
        db.Countries.Returns(countries.AsQueryable());
        db.CountryKapsarcSnapshots.Returns(snapshots.AsQueryable());
        return db;
    }
}
