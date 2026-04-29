using CCE.Application.Identity.Public;
using CCE.Application.Identity.Public.Queries.GetMyProfile;
using CCE.Domain.Identity;

namespace CCE.Application.Tests.Identity.Public.Queries;

public class GetMyProfileQueryHandlerTests
{
    [Fact]
    public async Task Returns_null_when_user_not_found()
    {
        var service = Substitute.For<IUserProfileService>();
        service.FindAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);
        var sut = new GetMyProfileQueryHandler(service);

        var result = await sut.Handle(new GetMyProfileQuery(System.Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Returns_profile_dto_when_user_found()
    {
        var userId = System.Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "alice@cce.local",
            UserName = "alice",
        };

        var service = Substitute.For<IUserProfileService>();
        service.FindAsync(userId, Arg.Any<CancellationToken>()).Returns(user);
        var sut = new GetMyProfileQueryHandler(service);

        var result = await sut.Handle(new GetMyProfileQuery(userId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(userId);
        result.Email.Should().Be("alice@cce.local");
        result.UserName.Should().Be("alice");
        result.LocalePreference.Should().Be("ar");
        result.KnowledgeLevel.Should().Be(KnowledgeLevel.Beginner);
        result.Interests.Should().BeEmpty();
    }
}
