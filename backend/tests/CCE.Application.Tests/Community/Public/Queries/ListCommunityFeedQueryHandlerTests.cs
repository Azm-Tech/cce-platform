using CCE.Application.Common.Interfaces;
using CCE.Application.Community;
using CCE.Application.Community.Public;
using CCE.Application.Community.Public.Queries.ListCommunityFeed;
using CCE.Application.Tests.Identity;
using CCE.Domain.Community;
using CCE.Domain.Identity;
using CCE.TestInfrastructure.Time;
using CommunityEntity = CCE.Domain.Community.Community;

namespace CCE.Application.Tests.Community.Public.Queries;

public class ListCommunityFeedQueryHandlerTests
{
    private static readonly FakeSystemClock Clock = new();

    private static Post MakePublishedPost(Guid communityId, Guid topicId)
    {
        var p = Post.CreateDraft(communityId, topicId, Guid.NewGuid(),
            PostType.Info, "Title", "Content", "en", Clock);
        p.Publish(Clock);
        return p;
    }

    private static (ListCommunityFeedQueryHandler Handler, IRedisFeedStore FeedStore)
        BuildSut(
            IEnumerable<Post>? posts = null,
            IEnumerable<CommunityEntity>? communities = null)
    {
        var db = Substitute.For<ICceDbContext>();
        db.Posts.Returns((posts ?? Array.Empty<Post>()).AsQueryable());
        db.Communities.Returns((communities ?? Array.Empty<CommunityEntity>()).AsQueryable());
        db.Users.Returns(Array.Empty<User>().AsQueryable());
        db.PostAttachments.Returns(Array.Empty<PostAttachment>().AsQueryable());
        db.Topics.Returns(Array.Empty<Topic>().AsQueryable());
        db.ExpertProfiles.Returns(Array.Empty<ExpertProfile>().AsQueryable());
        db.PostFollows.Returns(Array.Empty<PostFollow>().AsQueryable());
        db.PostVotes.Returns(Array.Empty<PostVote>().AsQueryable());

        var feedStore = Substitute.For<IRedisFeedStore>();
        feedStore.GetPostsMetaBatchAsync(
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, PostMeta>());

        var hydrator = new FeedHydratorService(db, feedStore);
        var handler = new ListCommunityFeedQueryHandler(db, feedStore, IdentityTestHelpers.BuildMsg(), hydrator);

        return (handler, feedStore);
    }

    [Fact]
    public async Task Redis_Hot_path_returns_result_from_GetHotPostsAsync()
    {
        var community = CommunityEntity.Create("اسم", "Name", "", "", "test-comm", CommunityVisibility.Public);
        var post = MakePublishedPost(community.Id, Guid.NewGuid());

        var (handler, feedStore) = BuildSut(new[] { post }, new[] { community });
        feedStore.GetHotPostsAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { post.Id });
        feedStore.GetHotLeaderboardCountAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(1L);

        var result = await handler.Handle(
            new ListCommunityFeedQuery(PostFeedSort.Hot, Array.Empty<Guid>(), community.Id, null, null, null, 1, 20),
            CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(1).And.Contain(dto => dto.Id == post.Id);
        await feedStore.Received(1).GetHotPostsAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Redis_Newest_path_calls_GetCommunityFeedAsync()
    {
        var community = CommunityEntity.Create("اسم", "Name", "", "", "test-comm", CommunityVisibility.Public);
        var post = MakePublishedPost(community.Id, Guid.NewGuid());

        var (handler, feedStore) = BuildSut(new[] { post }, new[] { community });
        feedStore.GetCommunityFeedAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { post.Id });
        feedStore.GetCommunityFeedCountAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(1L);

        var result = await handler.Handle(
            new ListCommunityFeedQuery(PostFeedSort.Newest, Array.Empty<Guid>(), community.Id, null, null, null, 1, 20),
            CancellationToken.None);

        result.Success.Should().BeTrue();
        await feedStore.Received(1).GetCommunityFeedAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TopVoted_sort_bypasses_Redis_and_uses_SQL()
    {
        var community = CommunityEntity.Create("اسم", "Name", "", "", "test-comm", CommunityVisibility.Public);
        var (handler, feedStore) = BuildSut(communities: new[] { community });

        await handler.Handle(
            new ListCommunityFeedQuery(PostFeedSort.TopVoted, Array.Empty<Guid>(), community.Id, null, null, null, 1, 20),
            CancellationToken.None);

        await feedStore.DidNotReceive().GetHotPostsAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        await feedStore.DidNotReceive().GetCommunityFeedAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Redis_cold_falls_through_to_SQL_and_returns_DB_post()
    {
        var community = CommunityEntity.Create("اسم", "Name", "", "", "test-comm", CommunityVisibility.Public);
        var post = MakePublishedPost(community.Id, Guid.NewGuid());

        var (handler, feedStore) = BuildSut(new[] { post }, new[] { community });
        // Redis returns empty → cold cache, fall through to SQL
        feedStore.GetHotPostsAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Guid>());

        var result = await handler.Handle(
            new ListCommunityFeedQuery(PostFeedSort.Hot, Array.Empty<Guid>(), community.Id, null, null, null, 1, 20),
            CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(1);
        result.Data.Items[0].Id.Should().Be(post.Id);
    }

    [Fact]
    public async Task TopicId_filter_restricts_Redis_overfetch_results_to_matching_topic()
    {
        var community = CommunityEntity.Create("اسم", "Name", "", "", "test-comm", CommunityVisibility.Public);
        var topicA = Guid.NewGuid();
        var topicB = Guid.NewGuid();
        var postA = MakePublishedPost(community.Id, topicA);
        var postB = MakePublishedPost(community.Id, topicB);

        var (handler, feedStore) = BuildSut(new[] { postA, postB }, new[] { community });
        // Redis returns both posts but only topicA is requested
        feedStore.GetHotPostsAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { postA.Id, postB.Id });
        feedStore.GetHotLeaderboardCountAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(2L);

        var result = await handler.Handle(
            new ListCommunityFeedQuery(PostFeedSort.Hot, Array.Empty<Guid>(), community.Id, topicA, null, null, 1, 20),
            CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(1);
        result.Data.Items[0].Id.Should().Be(postA.Id);
    }
}
