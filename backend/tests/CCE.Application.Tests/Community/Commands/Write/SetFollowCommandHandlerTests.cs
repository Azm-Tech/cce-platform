using CCE.Application.Common.Interfaces;
using CCE.Application.Community;
using CCE.Application.Community.Commands;
using CCE.Application.Community.Commands.SetCommunityFollow;
using CCE.Application.Community.Commands.SetPostFollow;
using CCE.Application.Community.Commands.SetTopicFollow;
using CCE.Application.Community.Commands.SetUserFollow;
using CCE.Application.Localization;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Community;
using CCE.Domain.Identity;

namespace CCE.Application.Tests.Community.Commands.Write;

public class SetFollowCommandHandlerTests
{
    private static ISystemClock MakeClock()
    {
        var clock = Substitute.For<ISystemClock>();
        clock.UtcNow.Returns(System.DateTimeOffset.UtcNow);
        return clock;
    }

    private static MessageFactory MakeMessages()
    {
        var localization = Substitute.For<ILocalizationService>();
        localization.GetString(Arg.Any<string>(), Arg.Any<string?>()).Returns(c => c.ArgAt<string>(0));
        return new MessageFactory(localization, Microsoft.Extensions.Logging.Abstractions.NullLogger<MessageFactory>.Instance);
    }

    private static ICurrentUserAccessor MakeUser(System.Guid id)
    {
        var u = Substitute.For<ICurrentUserAccessor>();
        u.GetUserId().Returns(id);
        return u;
    }

    private static Topic NewTopic()
        => Topic.Create("اسم", "Name", "وصف", "Desc", "my-topic", null, null, 0);

    private static Post NewPost(ISystemClock clock)
        => Post.CreateDraft(System.Guid.NewGuid(), System.Guid.NewGuid(), System.Guid.NewGuid(),
            PostType.Info, "Title", "Content", "en", clock);

    private static User NewUser(System.Guid id)
        => new() { Id = id, Email = $"{id:N}@x.io", UserName = id.ToString("N") };

    // ── SetTopicFollow ────────────────────────────────────────────────────────

    [Fact]
    public async Task SetTopicFollow_Followed_saves_new_follow()
    {
        var clock = MakeClock();
        var userId = System.Guid.NewGuid();
        var topic = NewTopic();

        var db = Substitute.For<ICceDbContext>();
        db.Topics.Returns(new[] { topic }.AsQueryable());
        var service = Substitute.For<ICommunityWriteService>();
        service.FindTopicFollowAsync(topic.Id, userId, Arg.Any<CancellationToken>()).Returns((TopicFollow?)null);

        var sut = new SetTopicFollowCommandHandler(service, db, MakeUser(userId), clock, MakeMessages());
        var result = await sut.Handle(new SetTopicFollowCommand(topic.Id, FollowStatus.Followed), CancellationToken.None);

        result.Success.Should().BeTrue();
        await service.Received(1).SaveFollowAsync(
            Arg.Is<TopicFollow>(f => f.TopicId == topic.Id && f.UserId == userId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetTopicFollow_Followed_idempotent_when_already_following()
    {
        var clock = MakeClock();
        var userId = System.Guid.NewGuid();
        var topic = NewTopic();

        var db = Substitute.For<ICceDbContext>();
        db.Topics.Returns(new[] { topic }.AsQueryable());
        var service = Substitute.For<ICommunityWriteService>();
        service.FindTopicFollowAsync(topic.Id, userId, Arg.Any<CancellationToken>())
            .Returns(TopicFollow.Follow(topic.Id, userId, clock));

        var sut = new SetTopicFollowCommandHandler(service, db, MakeUser(userId), clock, MakeMessages());
        var result = await sut.Handle(new SetTopicFollowCommand(topic.Id, FollowStatus.Followed), CancellationToken.None);

        result.Success.Should().BeTrue();
        await service.DidNotReceive().SaveFollowAsync(Arg.Any<TopicFollow>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetTopicFollow_Followed_returns_NotFound_when_topic_missing()
    {
        var userId = System.Guid.NewGuid();
        var db = Substitute.For<ICceDbContext>();
        db.Topics.Returns(System.Array.Empty<Topic>().AsQueryable());
        var service = Substitute.For<ICommunityWriteService>();

        var sut = new SetTopicFollowCommandHandler(service, db, MakeUser(userId), MakeClock(), MakeMessages());
        var result = await sut.Handle(new SetTopicFollowCommand(System.Guid.NewGuid(), FollowStatus.Followed), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Type.Should().Be(MessageType.NotFound);
        await service.DidNotReceive().SaveFollowAsync(Arg.Any<TopicFollow>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetTopicFollow_Unfollowed_calls_remove()
    {
        var userId = System.Guid.NewGuid();
        var topicId = System.Guid.NewGuid();

        var db = Substitute.For<ICceDbContext>();
        var service = Substitute.For<ICommunityWriteService>();
        service.RemoveTopicFollowAsync(topicId, userId, Arg.Any<CancellationToken>()).Returns(true);

        var sut = new SetTopicFollowCommandHandler(service, db, MakeUser(userId), MakeClock(), MakeMessages());
        var result = await sut.Handle(new SetTopicFollowCommand(topicId, FollowStatus.Unfollowed), CancellationToken.None);

        result.Success.Should().BeTrue();
        await service.Received(1).RemoveTopicFollowAsync(topicId, userId, Arg.Any<CancellationToken>());
    }

    // ── SetPostFollow ─────────────────────────────────────────────────────────

    [Fact]
    public async Task SetPostFollow_Followed_saves_new_follow()
    {
        var clock = MakeClock();
        var userId = System.Guid.NewGuid();
        var post = NewPost(clock);

        var db = Substitute.For<ICceDbContext>();
        db.Posts.Returns(new[] { post }.AsQueryable());
        var service = Substitute.For<ICommunityWriteService>();
        service.FindPostFollowAsync(post.Id, userId, Arg.Any<CancellationToken>()).Returns((PostFollow?)null);

        var sut = new SetPostFollowCommandHandler(service, db, MakeUser(userId), clock, MakeMessages());
        var result = await sut.Handle(new SetPostFollowCommand(post.Id, FollowStatus.Followed), CancellationToken.None);

        result.Success.Should().BeTrue();
        await service.Received(1).SaveFollowAsync(
            Arg.Is<PostFollow>(f => f.PostId == post.Id && f.UserId == userId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetPostFollow_Followed_returns_NotFound_when_post_missing()
    {
        var userId = System.Guid.NewGuid();
        var db = Substitute.For<ICceDbContext>();
        db.Posts.Returns(System.Array.Empty<Post>().AsQueryable());
        var service = Substitute.For<ICommunityWriteService>();

        var sut = new SetPostFollowCommandHandler(service, db, MakeUser(userId), MakeClock(), MakeMessages());
        var result = await sut.Handle(new SetPostFollowCommand(System.Guid.NewGuid(), FollowStatus.Followed), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Type.Should().Be(MessageType.NotFound);
    }

    [Fact]
    public async Task SetPostFollow_Unfollowed_calls_remove()
    {
        var userId = System.Guid.NewGuid();
        var postId = System.Guid.NewGuid();

        var db = Substitute.For<ICceDbContext>();
        var service = Substitute.For<ICommunityWriteService>();
        service.RemovePostFollowAsync(postId, userId, Arg.Any<CancellationToken>()).Returns(true);

        var sut = new SetPostFollowCommandHandler(service, db, MakeUser(userId), MakeClock(), MakeMessages());
        var result = await sut.Handle(new SetPostFollowCommand(postId, FollowStatus.Unfollowed), CancellationToken.None);

        result.Success.Should().BeTrue();
        await service.Received(1).RemovePostFollowAsync(postId, userId, Arg.Any<CancellationToken>());
    }

    // ── SetUserFollow ─────────────────────────────────────────────────────────

    [Fact]
    public async Task SetUserFollow_Followed_returns_400_on_self_follow()
    {
        var userId = System.Guid.NewGuid();
        var db = Substitute.For<ICceDbContext>();
        var service = Substitute.For<ICommunityWriteService>();

        var sut = new SetUserFollowCommandHandler(service, db, MakeUser(userId), MakeClock(), MakeMessages());
        var result = await sut.Handle(new SetUserFollowCommand(userId, FollowStatus.Followed), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Type.Should().Be(MessageType.Validation);
        await service.DidNotReceive().SaveFollowAsync(Arg.Any<UserFollow>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetUserFollow_Followed_saves_and_increments_counts()
    {
        var clock = MakeClock();
        var followerId = System.Guid.NewGuid();
        var followedId = System.Guid.NewGuid();
        var follower = NewUser(followerId);
        var followed = NewUser(followedId);

        var db = Substitute.For<ICceDbContext>();
        db.Users.Returns(new[] { follower, followed }.AsQueryable());
        var service = Substitute.For<ICommunityWriteService>();
        service.FindUserFollowAsync(followerId, followedId, Arg.Any<CancellationToken>()).Returns((UserFollow?)null);

        var sut = new SetUserFollowCommandHandler(service, db, MakeUser(followerId), clock, MakeMessages());
        var result = await sut.Handle(new SetUserFollowCommand(followedId, FollowStatus.Followed), CancellationToken.None);

        result.Success.Should().BeTrue();
        await service.Received(1).SaveFollowAsync(
            Arg.Is<UserFollow>(f => f.FollowerId == followerId && f.FollowedId == followedId), Arg.Any<CancellationToken>());
        follower.FollowingCount.Should().Be(1);
        followed.FollowerCount.Should().Be(1);
    }

    [Fact]
    public async Task SetUserFollow_Followed_returns_NotFound_when_target_user_missing()
    {
        var followerId = System.Guid.NewGuid();
        var db = Substitute.For<ICceDbContext>();
        db.Users.Returns(System.Array.Empty<User>().AsQueryable());
        var service = Substitute.For<ICommunityWriteService>();

        var sut = new SetUserFollowCommandHandler(service, db, MakeUser(followerId), MakeClock(), MakeMessages());
        var result = await sut.Handle(new SetUserFollowCommand(System.Guid.NewGuid(), FollowStatus.Followed), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Type.Should().Be(MessageType.NotFound);
    }

    [Fact]
    public async Task SetUserFollow_Unfollowed_removes_and_decrements_counts()
    {
        var followerId = System.Guid.NewGuid();
        var followedId = System.Guid.NewGuid();
        var follower = NewUser(followerId);
        var followed = NewUser(followedId);
        follower.IncrementFollowing();
        followed.IncrementFollowers();

        var db = Substitute.For<ICceDbContext>();
        db.Users.Returns(new[] { follower, followed }.AsQueryable());
        var service = Substitute.For<ICommunityWriteService>();
        service.RemoveUserFollowAsync(followerId, followedId, Arg.Any<CancellationToken>()).Returns(true);

        var sut = new SetUserFollowCommandHandler(service, db, MakeUser(followerId), MakeClock(), MakeMessages());
        var result = await sut.Handle(new SetUserFollowCommand(followedId, FollowStatus.Unfollowed), CancellationToken.None);

        result.Success.Should().BeTrue();
        follower.FollowingCount.Should().Be(0);
        followed.FollowerCount.Should().Be(0);
    }

    [Fact]
    public async Task SetUserFollow_Unfollowed_idempotent_when_not_following()
    {
        var followerId = System.Guid.NewGuid();
        var followedId = System.Guid.NewGuid();

        var db = Substitute.For<ICceDbContext>();
        var service = Substitute.For<ICommunityWriteService>();
        service.RemoveUserFollowAsync(followerId, followedId, Arg.Any<CancellationToken>()).Returns(false);

        var sut = new SetUserFollowCommandHandler(service, db, MakeUser(followerId), MakeClock(), MakeMessages());
        var result = await sut.Handle(new SetUserFollowCommand(followedId, FollowStatus.Unfollowed), CancellationToken.None);

        result.Success.Should().BeTrue();
        await db.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── SetCommunityFollow ────────────────────────────────────────────────────

    [Fact]
    public async Task SetCommunityFollow_Followed_adds_follow_and_increments()
    {
        var clock = MakeClock();
        var userId = System.Guid.NewGuid();
        var community = CCE.Domain.Community.Community.Create("اسم", "Name", "وصف", "Desc", "my-community", CommunityVisibility.Public);

        var repo = Substitute.For<ICommunityRepository>();
        repo.GetAsync(community.Id, Arg.Any<CancellationToken>()).Returns(community);
        repo.FindFollowAsync(community.Id, userId, Arg.Any<CancellationToken>()).Returns((CommunityFollow?)null);
        var db = Substitute.For<ICceDbContext>();

        var sut = new SetCommunityFollowCommandHandler(repo, db, MakeUser(userId), clock, MakeMessages());
        var result = await sut.Handle(new SetCommunityFollowCommand(community.Id, FollowStatus.Followed), CancellationToken.None);

        result.Success.Should().BeTrue();
        repo.Received(1).AddFollow(Arg.Is<CommunityFollow>(f => f.CommunityId == community.Id && f.UserId == userId));
        community.FollowerCount.Should().Be(1);
    }

    [Fact]
    public async Task SetCommunityFollow_Followed_returns_NotFound_when_community_missing()
    {
        var userId = System.Guid.NewGuid();
        var repo = Substitute.For<ICommunityRepository>();
        repo.GetAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>()).Returns((CCE.Domain.Community.Community?)null);
        var db = Substitute.For<ICceDbContext>();

        var sut = new SetCommunityFollowCommandHandler(repo, db, MakeUser(userId), MakeClock(), MakeMessages());
        var result = await sut.Handle(new SetCommunityFollowCommand(System.Guid.NewGuid(), FollowStatus.Followed), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Type.Should().Be(MessageType.NotFound);
        repo.DidNotReceive().AddFollow(Arg.Any<CommunityFollow>());
    }

    [Fact]
    public async Task SetCommunityFollow_Unfollowed_removes_and_decrements()
    {
        var clock = MakeClock();
        var userId = System.Guid.NewGuid();
        var community = CCE.Domain.Community.Community.Create("اسم", "Name", "وصف", "Desc", "my-community", CommunityVisibility.Public);
        community.IncrementFollowers();
        var follow = CommunityFollow.Follow(community.Id, userId, clock);

        var repo = Substitute.For<ICommunityRepository>();
        repo.FindFollowAsync(community.Id, userId, Arg.Any<CancellationToken>()).Returns(follow);
        repo.GetAsync(community.Id, Arg.Any<CancellationToken>()).Returns(community);
        var db = Substitute.For<ICceDbContext>();

        var sut = new SetCommunityFollowCommandHandler(repo, db, MakeUser(userId), clock, MakeMessages());
        var result = await sut.Handle(new SetCommunityFollowCommand(community.Id, FollowStatus.Unfollowed), CancellationToken.None);

        result.Success.Should().BeTrue();
        repo.Received(1).RemoveFollow(follow);
        community.FollowerCount.Should().Be(0);
    }
}
