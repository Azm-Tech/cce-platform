using CCE.Application.Identity.Public;
using CCE.Application.Identity.Public.Queries.GetMyProfile;
using CCE.Domain.Identity;
using static CCE.Application.Tests.Identity.IdentityTestHelpers;

namespace CCE.Application.Tests.Identity.Public.Queries;

public class GetMyProfileQueryHandlerTests
{
    [Fact]
    public async Task Returns_null_when_user_not_found()
    {
        var service = Substitute.For<IUserProfileRepository>();
        service.FindAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);
        var sut = new GetMyProfileQueryHandler(service, BuildErrors());

        var result = await sut.Handle(new GetMyProfileQuery(System.Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("IDENTITY_USER_NOT_FOUND");
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

        var service = Substitute.For<IUserProfileRepository>();
        service.FindAsync(userId, Arg.Any<CancellationToken>()).Returns(user);
        var sut = new GetMyProfileQueryHandler(service, BuildErrors());

        var result = await sut.Handle(new GetMyProfileQuery(userId), CancellationToken.None);

        result.Should().NotBeNull();
        result.Data!.Id.Should().Be(userId);
        result.Data.Email.Should().Be("alice@cce.local");
        result.Data.UserName.Should().Be("alice");
        result.Data.LocalePreference.Should().Be("ar");
        result.Data.KnowledgeLevel.Should().Be(KnowledgeLevel.Beginner);
        result.Data.Interests.Should().BeEmpty();
    }
}
