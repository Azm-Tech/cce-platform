using CCE.Application.Common.Interfaces;
using CCE.Application.Community;
using CCE.Application.Community.Public;
using CCE.Domain.Community;
using CCE.Domain.Identity;
using CCE.TestInfrastructure.Time;
using CommunityEntity = CCE.Domain.Community.Community;

namespace CCE.Application.Tests.Community.Public.Queries;

public class FeedHydratorServiceTests
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

    private static IRedisFeedStore BuildFeedStore()
    {
        var fs = Substitute.For<IRedisFeedStore>();
        fs.GetPostsMetaBatchAsync(
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, PostMeta>());
        return fs;
    }

    private static ICceDbContext BuildDb(
        IEnumerable<Post> posts,
        IEnumerable<CommunityEntity>? communities = null,
        IEnumerable<ExpertProfile>? experts = null,
        IEnumerable<PostFollow>? postFollows = null,
        IEnumerable<PostVote>? postVotes = null)
    {
        var db = Substitute.For<ICceDbContext>();
        db.Posts.Returns(posts.AsQueryable());
        db.Communities.Returns((communities ?? Array.Empty<CommunityEntity>()).AsQueryable());
        db.Users.Returns(Array.Empty<User>().AsQueryable());
        db.PostAttachments.Returns(Array.Empty<PostAttachment>().AsQueryable());
        db.Topics.Returns(Array.Empty<Topic>().AsQueryable());
        db.ExpertProfiles.Returns((experts ?? Array.Empty<ExpertProfile>()).AsQueryable());
        db.PostFollows.Returns((postFollows ?? Array.Empty<PostFollow>()).AsQueryable());
        db.PostVotes.Returns((postVotes ?? Array.Empty<PostVote>()).AsQueryable());
        return db;
    }

    [Fact]
    public async Task Returns_empty_when_orderedIds_is_empty()
    {
        var sut = new FeedHydratorService(BuildDb(Array.Empty<Post>()), BuildFeedStore());
        var result = await sut.HydrateAsync(Array.Empty<Guid>(), null, null, CancellationToken.None);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Filters_out_draft_post()
    {
        var community = CommunityEntity.Create("اسم", "Name", "", "", "test-comm", CommunityVisibility.Public);
        var draft = Post.CreateDraft(community.Id, Guid.NewGuid(), Guid.NewGuid(),
            PostType.Info, "Draft", "Content", "en", Clock);  // not published

        var sut = new FeedHydratorService(BuildDb(new[] { draft }, new[] { community }), BuildFeedStore());
        var result = await sut.HydrateAsync(new[] { draft.Id }, null, null, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Filters_out_post_in_private_community()
    {
        var privateCommunity = CommunityEntity.Create("اسم", "Name", "", "", "priv-comm", CommunityVisibility.Private);
        var post = MakePublishedPost(privateCommunity.Id, Guid.NewGuid());

        var sut = new FeedHydratorService(BuildDb(new[] { post }, new[] { privateCommunity }), BuildFeedStore());
        var result = await sut.HydrateAsync(new[] { post.Id }, null, null, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task TopicFilter_removes_posts_from_other_topics()
    {
        var community = CommunityEntity.Create("اسم", "Name", "", "", "test-comm", CommunityVisibility.Public);
        var topicA = Guid.NewGuid();
        var topicB = Guid.NewGuid();
        var postA = MakePublishedPost(community.Id, topicA);
        var postB = MakePublishedPost(community.Id, topicB);

        var sut = new FeedHydratorService(BuildDb(new[] { postA, postB }, new[] { community }), BuildFeedStore());
        var result = await sut.HydrateAsync(new[] { postA.Id, postB.Id }, null, topicA, CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Id.Should().Be(postA.Id);
    }

    [Fact]
    public async Task Preserves_orderedIds_order_regardless_of_DB_order()
    {
        var community = CommunityEntity.Create("اسم", "Name", "", "", "test-comm", CommunityVisibility.Public);
        var topicId = Guid.NewGuid();
        var first = MakePublishedPost(community.Id, topicId);
        var second = MakePublishedPost(community.Id, topicId);

        // db list is [first, second] but request order is [second, first]
        var sut = new FeedHydratorService(BuildDb(new[] { first, second }, new[] { community }), BuildFeedStore());
        var result = await sut.HydrateAsync(new[] { second.Id, first.Id }, null, null, CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].Id.Should().Be(second.Id);
        result[1].Id.Should().Be(first.Id);
    }

    [Fact]
    public async Task Returns_empty_user_specific_fields_when_userId_is_null()
    {
        var community = CommunityEntity.Create("اسم", "Name", "", "", "test-comm", CommunityVisibility.Public);
        var someUser = Guid.NewGuid();
        var post = MakePublishedPost(community.Id, Guid.NewGuid());
        var follow = PostFollow.Follow(post.Id, someUser, Clock);
        var vote = PostVote.Cast(post.Id, someUser, 1, Clock);

        var sut = new FeedHydratorService(
            BuildDb(new[] { post }, new[] { community },
                postFollows: new[] { follow },
                postVotes: new[] { vote }),
            BuildFeedStore());

        var result = await sut.HydrateAsync(new[] { post.Id }, userId: null, null, CancellationToken.None);

        result[0].IsWatchlisted.Should().BeFalse();
        result[0].VoteStatus.Should().Be(0);
    }

    [Fact]
    public async Task Sets_IsWatchlisted_and_VoteStatus_for_authenticated_user()
    {
        var community = CommunityEntity.Create("اسم", "Name", "", "", "test-comm", CommunityVisibility.Public);
        var userId = Guid.NewGuid();
        var post = MakePublishedPost(community.Id, Guid.NewGuid());
        var follow = PostFollow.Follow(post.Id, userId, Clock);
        var vote = PostVote.Cast(post.Id, userId, 1, Clock);

        var sut = new FeedHydratorService(
            BuildDb(new[] { post }, new[] { community },
                postFollows: new[] { follow },
                postVotes: new[] { vote }),
            BuildFeedStore());

        var result = await sut.HydrateAsync(new[] { post.Id }, userId, null, CancellationToken.None);

        result[0].IsWatchlisted.Should().BeTrue();
        result[0].VoteStatus.Should().Be(1);
    }

    [Fact]
    public async Task Sets_IsExpert_when_author_has_expert_profile()
    {
        var community = CommunityEntity.Create("اسم", "Name", "", "", "test-comm", CommunityVisibility.Public);
        var expertAuthorId = Guid.NewGuid();
        var post = MakePublishedPost(community.Id, Guid.NewGuid(), expertAuthorId);
        var expert = MakeExpertProfile(expertAuthorId);

        var sut = new FeedHydratorService(
            BuildDb(new[] { post }, new[] { community }, experts: new[] { expert }),
            BuildFeedStore());

        var result = await sut.HydrateAsync(new[] { post.Id }, null, null, CancellationToken.None);

        result[0].IsExpert.Should().BeTrue();
    }
}
