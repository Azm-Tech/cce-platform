using CCE.Application.Identity.Public;
using CCE.Application.Identity.Public.Commands.UpdateMyProfile;
using CCE.Domain.Identity;
using static CCE.Application.Tests.Identity.IdentityTestHelpers;

namespace CCE.Application.Tests.Identity.Public.Commands;

public class UpdateMyProfileCommandHandlerTests
{
    [Fact]
    public async Task Returns_null_when_user_not_found()
    {
        var service = Substitute.For<IUserProfileRepository>();
        service.FindAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);
        var sut = new UpdateMyProfileCommandHandler(service, BuildErrors());

        var cmd = new UpdateMyProfileCommand(
            System.Guid.NewGuid(), "en", KnowledgeLevel.Intermediate,
            System.Array.Empty<string>(), null, null);

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("IDENTITY_USER_NOT_FOUND");
        await service.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
    }

    [Fact]
    public async Task Updates_and_returns_dto_when_user_found()
    {
        var userId = System.Guid.NewGuid();
        var countryId = System.Guid.NewGuid();
        var user = new User { Id = userId, Email = "alice@cce.local", UserName = "alice" };

        var service = Substitute.For<IUserProfileRepository>();
        service.FindAsync(userId, Arg.Any<CancellationToken>()).Returns(user);
        service.UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>()).Returns(System.Threading.Tasks.Task.CompletedTask);
        var sut = new UpdateMyProfileCommandHandler(service, BuildErrors());

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
        await service.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Clears_country_when_country_id_is_null()
    {
        var userId = System.Guid.NewGuid();
        var user = new User { Id = userId };
        user.AssignCountry(System.Guid.NewGuid());

        var service = Substitute.For<IUserProfileRepository>();
        service.FindAsync(userId, Arg.Any<CancellationToken>()).Returns(user);
        service.UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>()).Returns(System.Threading.Tasks.Task.CompletedTask);
        var sut = new UpdateMyProfileCommandHandler(service, BuildErrors());

        var cmd = new UpdateMyProfileCommand(
            userId, "ar", KnowledgeLevel.Beginner,
            System.Array.Empty<string>(), null, null);

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.Data!.CountryId.Should().BeNull();
    }
}
