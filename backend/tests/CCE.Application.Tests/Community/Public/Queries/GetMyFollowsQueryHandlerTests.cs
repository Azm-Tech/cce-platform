using CCE.Application.Common.Interfaces;
using CCE.Application.Community.Public.Queries.GetMyFollows;
using CCE.Domain.Common;
using CCE.Domain.Community;

namespace CCE.Application.Tests.Community.Public.Queries;

public class GetMyFollowsQueryHandlerTests
{
    private static ISystemClock MakeClock()
    {
        var clock = Substitute.For<ISystemClock>();
        clock.UtcNow.Returns(System.DateTimeOffset.UtcNow);
        return clock;
    }

    [Fact]
    public async Task Returns_all_three_follow_lists_for_user()
    {
        var clock = MakeClock();
        var userId = System.Guid.NewGuid();
        var topicId = System.Guid.NewGuid();
        var followedUserId = System.Guid.NewGuid();
        var postId = System.Guid.NewGuid();

        var topicFollow = TopicFollow.Follow(topicId, userId, clock);
        var userFollow = UserFollow.Follow(userId, followedUserId, clock);
        var postFollow = PostFollow.Follow(postId, userId, clock);

        var db = BuildDb(userId, new[] { topicFollow }, new[] { userFollow }, new[] { postFollow });
        var sut = new GetMyFollowsQueryHandler(db);

        var result = await sut.Handle(new GetMyFollowsQuery(userId), CancellationToken.None);

        result.TopicIds.Should().ContainSingle().Which.Should().Be(topicId);
        result.UserIds.Should().ContainSingle().Which.Should().Be(followedUserId);
        result.PostIds.Should().ContainSingle().Which.Should().Be(postId);
    }

    [Fact]
    public async Task Returns_empty_lists_when_user_has_no_follows()
    {
        var userId = System.Guid.NewGuid();
        var db = BuildDb(userId,
            System.Array.Empty<TopicFollow>(),
            System.Array.Empty<UserFollow>(),
            System.Array.Empty<PostFollow>());
        var sut = new GetMyFollowsQueryHandler(db);

        var result = await sut.Handle(new GetMyFollowsQuery(userId), CancellationToken.None);

        result.TopicIds.Should().BeEmpty();
        result.UserIds.Should().BeEmpty();
        result.PostIds.Should().BeEmpty();
    }

    [Fact]
    public async Task Excludes_follows_belonging_to_other_users()
    {
        var clock = MakeClock();
        var userId = System.Guid.NewGuid();
        var otherUserId = System.Guid.NewGuid();
        var topicId = System.Guid.NewGuid();

        var myFollow = TopicFollow.Follow(topicId, userId, clock);
        var otherFollow = TopicFollow.Follow(System.Guid.NewGuid(), otherUserId, clock);

        var db = BuildDb(userId,
            new[] { myFollow, otherFollow },
            System.Array.Empty<UserFollow>(),
            System.Array.Empty<PostFollow>());
        var sut = new GetMyFollowsQueryHandler(db);

        var result = await sut.Handle(new GetMyFollowsQuery(userId), CancellationToken.None);

        result.TopicIds.Should().ContainSingle().Which.Should().Be(topicId);
    }

    private static ICceDbContext BuildDb(
        System.Guid userId,
        IEnumerable<TopicFollow> topicFollows,
        IEnumerable<UserFollow> userFollows,
        IEnumerable<PostFollow> postFollows)
    {
        var db = Substitute.For<ICceDbContext>();
        db.TopicFollows.Returns(topicFollows.AsQueryable());
        db.UserFollows.Returns(userFollows.AsQueryable());
        db.PostFollows.Returns(postFollows.AsQueryable());
        return db;
    }
}
