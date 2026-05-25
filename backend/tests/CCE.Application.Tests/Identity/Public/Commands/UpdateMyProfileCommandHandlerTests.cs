using CCE.Application.Common.Interfaces;
using CCE.Application.Identity.Public;
using CCE.Application.Identity.Public.Commands.UpdateMyProfile;
using CCE.Application.Messages;
using CCE.Domain.Identity;
using static CCE.Application.Tests.Identity.IdentityTestHelpers;

namespace CCE.Application.Tests.Identity.Public.Commands;

public class UpdateMyProfileCommandHandlerTests
{
    [Fact]
    public async Task Returns_null_when_user_not_found()
    {
        var db = Substitute.For<ICceDbContext>();
        var service = Substitute.For<IUserProfileRepository>();
        service.FindAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);
        var sut = new UpdateMyProfileCommandHandler(db, service, BuildMsg());

        var cmd = new UpdateMyProfileCommand(
            System.Guid.NewGuid(), "en", KnowledgeLevel.Intermediate,
            System.Array.Empty<string>(), null, null);

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Code.Should().Be(SystemCode.ERR001);
        service.DidNotReceiveWithAnyArgs().Update(default!);
        await db.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Updates_and_returns_dto_when_user_found()
    {
        var userId = System.Guid.NewGuid();
        var countryId = System.Guid.NewGuid();
        var user = new User { Id = userId, Email = "alice@cce.local", UserName = "alice" };

        var db = Substitute.For<ICceDbContext>();
        var service = Substitute.For<IUserProfileRepository>();
        service.FindAsync(userId, Arg.Any<CancellationToken>()).Returns(user);
        var sut = new UpdateMyProfileCommandHandler(db, service, BuildMsg());

        var cmd = new UpdateMyProfileCommand(
            userId, "en", KnowledgeLevel.Advanced,
            new[] { "Hydrogen", "Solar" },
            "https://cdn.example.com/avatar.png",
            countryId);

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.Data!.LocalePreference.Should().Be("en");
        result.Data.KnowledgeLevel.Should().Be(KnowledgeLevel.Advanced);
        result.Data.Interests.Should().BeEquivalentTo(new[] { "Hydrogen", "Solar" });
        result.Data.AvatarUrl.Should().Be("https://cdn.example.com/avatar.png");
        result.Data.CountryId.Should().Be(countryId);
        service.Received(1).Update(user);
        await db.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Clears_country_when_country_id_is_null()
    {
        var userId = System.Guid.NewGuid();
        var user = new User { Id = userId };
        user.AssignCountry(System.Guid.NewGuid());

        var db = Substitute.For<ICceDbContext>();
        var service = Substitute.For<IUserProfileRepository>();
        service.FindAsync(userId, Arg.Any<CancellationToken>()).Returns(user);
        var sut = new UpdateMyProfileCommandHandler(db, service, BuildMsg());

        var cmd = new UpdateMyProfileCommand(
            userId, "ar", KnowledgeLevel.Beginner,
            System.Array.Empty<string>(), null, null);

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.Data!.CountryId.Should().BeNull();
    }
}
