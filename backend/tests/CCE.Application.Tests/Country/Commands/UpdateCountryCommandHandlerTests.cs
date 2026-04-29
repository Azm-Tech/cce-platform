using CCE.Application.Country;
using CCE.Application.Country.Commands.UpdateCountry;

namespace CCE.Application.Tests.Country.Commands;

public class UpdateCountryCommandHandlerTests
{
    [Fact]
    public async Task Returns_null_when_country_not_found()
    {
        var service = Substitute.For<ICountryAdminService>();
        service.FindAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>()).Returns((CCE.Domain.Country.Country?)null);
        var sut = new UpdateCountryCommandHandler(service);

        var result = await sut.Handle(BuildCommand(System.Guid.NewGuid(), isActive: true), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Updates_names_and_calls_UpdateAsync()
    {
        var country = CCE.Domain.Country.Country.Register("USA", "US", "أمريكا", "United States", "أمريكا الشمالية", "North America", "https://example/flag.png");
        var service = Substitute.For<ICountryAdminService>();
        service.FindAsync(country.Id, Arg.Any<CancellationToken>()).Returns(country);
        var sut = new UpdateCountryCommandHandler(service);

        var cmd = new UpdateCountryCommand(country.Id, "الولايات المتحدة", "USA Updated", "أمريكا الشمالية", "North America", true);
        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result!.NameAr.Should().Be("الولايات المتحدة");
        result.NameEn.Should().Be("USA Updated");
        result.IsActive.Should().BeTrue();
        await service.Received(1).UpdateAsync(country, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deactivates_when_IsActive_is_false()
    {
        var country = CCE.Domain.Country.Country.Register("USA", "US", "أمريكا", "United States", "أمريكا الشمالية", "North America", "https://example/flag.png");
        var service = Substitute.For<ICountryAdminService>();
        service.FindAsync(country.Id, Arg.Any<CancellationToken>()).Returns(country);
        var sut = new UpdateCountryCommandHandler(service);

        var cmd = new UpdateCountryCommand(country.Id, "أمريكا", "United States", "أمريكا الشمالية", "North America", false);
        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result!.IsActive.Should().BeFalse();
        country.IsActive.Should().BeFalse();
    }

    private static UpdateCountryCommand BuildCommand(System.Guid id, bool isActive) =>
        new(id, "أمريكا", "United States", "أمريكا الشمالية", "North America", isActive);
}
