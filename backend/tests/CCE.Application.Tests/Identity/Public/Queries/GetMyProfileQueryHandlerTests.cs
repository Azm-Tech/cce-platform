using CCE.Application.Common.Interfaces;
using CCE.Application.Identity.Public.Queries.GetMyProfile;
using CCE.Application.Messages;
using CCE.Domain.Identity;
using static CCE.Application.Tests.Identity.IdentityTestHelpers;

namespace CCE.Application.Tests.Identity.Public.Queries;

public class GetMyProfileQueryHandlerTests
{
    [Fact]
    public async Task Returns_null_when_user_not_found()
    {
        var db = Substitute.For<ICceDbContext>();
        db.Users.Returns(System.Array.Empty<User>().AsQueryable());
        var sut = new GetMyProfileQueryHandler(db, BuildMsg());

        var result = await sut.Handle(new GetMyProfileQuery(System.Guid.NewGuid()), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Code.Should().Be(SystemCode.ERR001);
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

        var db = Substitute.For<ICceDbContext>();
        db.Users.Returns(new[] { user }.AsQueryable());
        var sut = new GetMyProfileQueryHandler(db, BuildMsg());

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
