using CCE.Application.Common.Interfaces;
using CCE.Application.Community;
using CCE.Application.Community.Public;
using CCE.Application.Community.Public.Queries.ListCommunityFeed;
using CCE.Application.Community.Public.Queries.ListUserFeed;
using CCE.Application.Tests.Identity;
using CCE.Domain.Community;
using CCE.Domain.Identity;
using CCE.TestInfrastructure.Time;
using CommunityEntity = CCE.Domain.Community.Community;

namespace CCE.Application.Tests.Community.Public.Queries;

public class ListUserFeedQueryHandlerTests
{
    private static readonly FakeSystemClock Clock = new();

    private static Post MakePublishedPost(Guid communityId, Guid topicId, Guid? authorId = null)
    {
        var p = Post.CreateDraft(communityId, topicId, authorId ?? Guid.NewGuid(),
            PostType.Info, "Title", "Content", "en", Clock);
        p.Publish(Clock);
        return p;
    }

    private static ExpertProfile MakeExpertProfile(Guid userId)
    {
        var req = ExpertRegistrationRequest.Submit(
            userId, "bio ar", "bio en", new[] { "AI" }, Guid.NewGuid(), Clock);
        req.Approve(Guid.NewGuid(), Clock);
        return ExpertProfile.CreateFromApprovedRequest(req, "Dr", "Dr", Clock);
    }

    private static (ListUserFeedQueryHandler Handler, IRedisFeedStore FeedStore)
        BuildSut(
            IEnumerable<Post>? posts = null,
            IEnumerable<CommunityEntity>? communities = null,
            IEnumerable<CommunityFollow>? communityFollows = null,
            IEnumerable<UserFollow>? userFollows = null,
            IEnumerable<TopicFollow>? topicFollows = null,
            IEnumerable<ExpertProfile>? experts = null)
    {
        var db = Substitute.For<ICceDbContext>();
        db.Posts.Returns((posts ?? Array.Empty<Post>()).AsQueryable());
        db.Communities.Returns((communities ?? Array.Empty<CommunityEntity>()).AsQueryable());
        db.CommunityFollows.Returns((communityFollows ?? Array.Empty<CommunityFollow>()).AsQueryable());
        db.UserFollows.Returns((userFollows ?? Array.Empty<UserFollow>()).AsQueryable());
        db.TopicFollows.Returns((topicFollows ?? Array.Empty<TopicFollow>()).AsQueryable());
        db.Users.Returns(Array.Empty<User>().AsQueryable());
        db.PostAttachments.Returns(Array.Empty<PostAttachment>().AsQueryable());
        db.Topics.Returns(Array.Empty<Topic>().AsQueryable());
        db.ExpertProfiles.Returns((experts ?? Array.Empty<ExpertProfile>()).AsQueryable());
        db.PostFollows.Returns(Array.Empty<PostFollow>().AsQueryable());
        db.PostVotes.Returns(Array.Empty<PostVote>().AsQueryable());

        var feedStore = Substitute.For<IRedisFeedStore>();
        feedStore.GetPostsMetaBatchAsync(
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, PostMeta>());

        var hydrator = new FeedHydratorService(db, feedStore);
        var handler = new ListUserFeedQueryHandler(db, feedStore, IdentityTestHelpers.BuildMsg(), hydrator);

        return (handler, feedStore);
    }

    [Fact]
    public async Task Community_Redis_Hot_path_taken_when_user_follows_community()
    {
        var userId = Guid.NewGuid();
        var community = CommunityEntity.Create("اسم", "Name", "", "", "test-comm", CommunityVisibility.Public);
        var post = MakePublishedPost(community.Id, Guid.NewGuid());
        var follow = CommunityFollow.Follow(community.Id, userId, Clock);

        var (handler, feedStore) = BuildSut(
            new[] { post }, new[] { community }, communityFollows: new[] { follow });

        feedStore.GetHotPostsAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { post.Id });
        feedStore.GetHotLeaderboardCountAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(1L);

        var result = await handler.Handle(
            new ListUserFeedQuery(userId, PostFeedSort.Hot, Array.Empty<Guid>(), community.Id, null, null, 1, 20),
            CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(1);
        await feedStore.Received(1).GetHotPostsAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Community_Redis_path_skipped_when_user_does_not_follow_community()
    {
        var userId = Guid.NewGuid();
        var community = CommunityEntity.Create("اسم", "Name", "", "", "test-comm", CommunityVisibility.Public);
        // No CommunityFollow → canUseCommunityRedis = false

        var (handler, feedStore) = BuildSut(communities: new[] { community });

        await handler.Handle(
            new ListUserFeedQuery(userId, PostFeedSort.Hot, Array.Empty<Guid>(), community.Id, null, null, 1, 20),
            CancellationToken.None);

        await feedStore.DidNotReceive().GetHotPostsAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Personal_Redis_path_taken_for_unfiltered_Newest_feed()
    {
        var userId = Guid.NewGuid();
        var community = CommunityEntity.Create("اسم", "Name", "", "", "test-comm", CommunityVisibility.Public);
        var post = MakePublishedPost(community.Id, Guid.NewGuid());

        var (handler, feedStore) = BuildSut(new[] { post }, new[] { community });

        feedStore.GetUserFeedWithScoresAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<(Guid PostId, DateTimeOffset PublishedOn)>
            {
                (post.Id, Clock.UtcNow)
            });
        feedStore.GetUserFeedCountAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(1L);

        var result = await handler.Handle(
            new ListUserFeedQuery(userId, PostFeedSort.Newest, Array.Empty<Guid>(), null, null, null, 1, 20),
            CancellationToken.None);

        result.Success.Should().BeTrue();
        await feedStore.Received(1).GetUserFeedWithScoresAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FallbackSql_returns_empty_when_user_has_no_follow_graph()
    {
        var userId = Guid.NewGuid();
        // No follows at all → FallbackSqlAsync exits early with empty result

        var (handler, _) = BuildSut();

        var result = await handler.Handle(
            new ListUserFeedQuery(userId, PostFeedSort.Hot, Array.Empty<Guid>(), null, null, null, 1, 20),
            CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().BeEmpty();
        result.Data.Total.Should().Be(0);
    }

    [Fact]
    public async Task FallbackSql_returns_empty_when_communityFilter_is_not_in_follow_graph()
    {
        var userId = Guid.NewGuid();
        // User follows one community but the query filters for a different (unfollowed) community.
        // FallbackSqlAsync's short-circuit: communityFilter not followed + no user/topic follows → empty.
        var followedCommunity = CommunityEntity.Create("أخرى", "Other", "", "", "other-comm", CommunityVisibility.Public);
        var unfollowedCommunityId = Guid.NewGuid();
        var follow = CommunityFollow.Follow(followedCommunity.Id, userId, Clock);

        var (handler, feedStore) = BuildSut(
            communities: new[] { followedCommunity },
            communityFollows: new[] { follow });

        var result = await handler.Handle(
            new ListUserFeedQuery(userId, PostFeedSort.Newest, Array.Empty<Guid>(), unfollowedCommunityId, null, null, 1, 20),
            CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().BeEmpty();
        await feedStore.DidNotReceive().GetUserFeedWithScoresAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }
}
