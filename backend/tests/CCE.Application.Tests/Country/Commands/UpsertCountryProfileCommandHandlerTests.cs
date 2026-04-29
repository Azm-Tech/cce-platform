using CCE.Application.Common.Interfaces;
using CCE.Application.Country;
using CCE.Application.Country.Commands.UpsertCountryProfile;
using CCE.Domain.Common;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Country.Commands;

public class UpsertCountryProfileCommandHandlerTests
{
    private static readonly FakeSystemClock Clock = new();

    [Fact]
    public async Task Creates_profile_when_none_exists()
    {
        var countryId = System.Guid.NewGuid();
        var adminId = System.Guid.NewGuid();
        var service = Substitute.For<ICountryProfileService>();
        service.FindByCountryIdAsync(countryId, Arg.Any<CancellationToken>())
            .Returns((CCE.Domain.Country.CountryProfile?)null);
        var sut = BuildSut(service, adminId);

        var cmd = BuildCommand(countryId);
        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.CountryId.Should().Be(countryId);
        result.DescriptionEn.Should().Be("en-desc");
        await service.Received(1).SaveAsync(Arg.Any<CCE.Domain.Country.CountryProfile>(), Arg.Any<CancellationToken>());
        await service.DidNotReceive().UpdateAsync(Arg.Any<CCE.Domain.Country.CountryProfile>(), Arg.Any<byte[]>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Updates_profile_when_existing_found()
    {
        var countryId = System.Guid.NewGuid();
        var adminId = System.Guid.NewGuid();
        var existing = CCE.Domain.Country.CountryProfile.Create(
            countryId, "old-ar", "old-en", "old-init-ar", "old-init-en", null, null, adminId, Clock);
        var rowVersion = new byte[] { 1, 2, 3, 4 };

        var service = Substitute.For<ICountryProfileService>();
        service.FindByCountryIdAsync(countryId, Arg.Any<CancellationToken>()).Returns(existing);
        var sut = BuildSut(service, adminId);

        var cmd = new UpsertCountryProfileCommand(
            countryId, "new-ar", "new-en", "new-init-ar", "new-init-en", null, null, rowVersion);
        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.DescriptionEn.Should().Be("new-en");
        result.DescriptionAr.Should().Be("new-ar");
        await service.DidNotReceive().SaveAsync(Arg.Any<CCE.Domain.Country.CountryProfile>(), Arg.Any<CancellationToken>());
        await service.Received(1).UpdateAsync(existing, rowVersion, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_DomainException_when_no_actor()
    {
        var service = Substitute.For<ICountryProfileService>();
        service.FindByCountryIdAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>())
            .Returns((CCE.Domain.Country.CountryProfile?)null);
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns((System.Guid?)null);
        var sut = new UpsertCountryProfileCommandHandler(service, currentUser, Clock);

        var act = async () => await sut.Handle(BuildCommand(System.Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Propagates_ConcurrencyException_from_UpdateAsync()
    {
        var countryId = System.Guid.NewGuid();
        var adminId = System.Guid.NewGuid();
        var existing = CCE.Domain.Country.CountryProfile.Create(
            countryId, "ar-desc", "en-desc", "ar-init", "en-init", null, null, adminId, Clock);

        var service = Substitute.For<ICountryProfileService>();
        service.FindByCountryIdAsync(countryId, Arg.Any<CancellationToken>()).Returns(existing);
        service.UpdateAsync(default!, default!, default).ReturnsForAnyArgs<Task>(_ =>
            throw new ConcurrencyException("conflict"));
        var sut = BuildSut(service, adminId);

        var act = async () => await sut.Handle(BuildCommand(countryId), CancellationToken.None);

        await act.Should().ThrowAsync<ConcurrencyException>();
    }

    private static UpsertCountryProfileCommandHandler BuildSut(
        ICountryProfileService service,
        System.Guid? userId = null)
    {
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns(userId ?? System.Guid.NewGuid());
        return new UpsertCountryProfileCommandHandler(service, currentUser, Clock);
    }

    private static UpsertCountryProfileCommand BuildCommand(System.Guid countryId) =>
        new(countryId, "ar-desc", "en-desc", "ar-init", "en-init", null, null, System.Array.Empty<byte>());
}
