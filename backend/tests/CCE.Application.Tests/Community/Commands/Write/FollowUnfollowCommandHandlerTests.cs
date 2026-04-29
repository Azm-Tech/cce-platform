using CCE.Application.Common.Interfaces;
using CCE.Application.Community;
using CCE.Application.Community.Commands.FollowPost;
using CCE.Application.Community.Commands.FollowTopic;
using CCE.Application.Community.Commands.FollowUser;
using CCE.Application.Community.Commands.UnfollowPost;
using CCE.Application.Community.Commands.UnfollowTopic;
using CCE.Application.Community.Commands.UnfollowUser;
using CCE.Domain.Common;
using CCE.Domain.Community;

namespace CCE.Application.Tests.Community.Commands.Write;

public class FollowUnfollowCommandHandlerTests
{
    private static ISystemClock MakeClock()
    {
        var clock = Substitute.For<ISystemClock>();
        clock.UtcNow.Returns(System.DateTimeOffset.UtcNow);
        return clock;
    }

    // ── FollowTopic ──────────────────────────────────────────────────────────

    [Fact]
    public async Task FollowTopic_saves_new_follow()
    {
        var clock = MakeClock();
        var userId = System.Guid.NewGuid();
        var topicId = System.Guid.NewGuid();

        var service = Substitute.For<ICommunityWriteService>();
        service.FindTopicFollowAsync(topicId, userId, Arg.Any<CancellationToken>())
            .Returns((TopicFollow?)null);
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns(userId);

        var sut = new FollowTopicCommandHandler(service, currentUser, clock);
        await sut.Handle(new FollowTopicCommand(topicId), CancellationToken.None);

        await service.Received(1).SaveFollowAsync(
            Arg.Is<TopicFollow>(f => f.TopicId == topicId && f.UserId == userId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FollowTopic_idempotent_when_already_following()
    {
        var clock = MakeClock();
        var userId = System.Guid.NewGuid();
        var topicId = System.Guid.NewGuid();
        var existing = TopicFollow.Follow(topicId, userId, clock);

        var service = Substitute.For<ICommunityWriteService>();
        service.FindTopicFollowAsync(topicId, userId, Arg.Any<CancellationToken>())
            .Returns(existing);
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns(userId);

        var sut = new FollowTopicCommandHandler(service, currentUser, clock);
        await sut.Handle(new FollowTopicCommand(topicId), CancellationToken.None);

        await service.DidNotReceive().SaveFollowAsync(Arg.Any<TopicFollow>(), Arg.Any<CancellationToken>());
    }

    // ── UnfollowTopic ────────────────────────────────────────────────────────

    [Fact]
    public async Task UnfollowTopic_calls_remove()
    {
        var userId = System.Guid.NewGuid();
        var topicId = System.Guid.NewGuid();

        var service = Substitute.For<ICommunityWriteService>();
        service.RemoveTopicFollowAsync(topicId, userId, Arg.Any<CancellationToken>()).Returns(true);
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns(userId);

        var sut = new UnfollowTopicCommandHandler(service, currentUser);
        await sut.Handle(new UnfollowTopicCommand(topicId), CancellationToken.None);

        await service.Received(1).RemoveTopicFollowAsync(topicId, userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UnfollowTopic_idempotent_when_not_following()
    {
        var userId = System.Guid.NewGuid();
        var topicId = System.Guid.NewGuid();

        var service = Substitute.For<ICommunityWriteService>();
        service.RemoveTopicFollowAsync(topicId, userId, Arg.Any<CancellationToken>()).Returns(false);
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns(userId);

        var sut = new UnfollowTopicCommandHandler(service, currentUser);

        // Should not throw even when row is absent
        var act = async () => await sut.Handle(new UnfollowTopicCommand(topicId), CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    // ── FollowUser ───────────────────────────────────────────────────────────

    [Fact]
    public async Task FollowUser_saves_new_follow()
    {
        var clock = MakeClock();
        var followerId = System.Guid.NewGuid();
        var followedId = System.Guid.NewGuid();

        var service = Substitute.For<ICommunityWriteService>();
        service.FindUserFollowAsync(followerId, followedId, Arg.Any<CancellationToken>())
            .Returns((UserFollow?)null);
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns(followerId);

        var sut = new FollowUserCommandHandler(service, currentUser, clock);
        await sut.Handle(new FollowUserCommand(followedId), CancellationToken.None);

        await service.Received(1).SaveFollowAsync(
            Arg.Is<UserFollow>(f => f.FollowerId == followerId && f.FollowedId == followedId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FollowUser_throws_DomainException_on_self_follow()
    {
        var clock = MakeClock();
        var userId = System.Guid.NewGuid();

        var service = Substitute.For<ICommunityWriteService>();
        service.FindUserFollowAsync(userId, userId, Arg.Any<CancellationToken>())
            .Returns((UserFollow?)null);
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns(userId);

        var sut = new FollowUserCommandHandler(service, currentUser, clock);

        var act = async () => await sut.Handle(new FollowUserCommand(userId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*themselves*");
    }

    // ── UnfollowUser ─────────────────────────────────────────────────────────

    [Fact]
    public async Task UnfollowUser_calls_remove()
    {
        var followerId = System.Guid.NewGuid();
        var followedId = System.Guid.NewGuid();

        var service = Substitute.For<ICommunityWriteService>();
        service.RemoveUserFollowAsync(followerId, followedId, Arg.Any<CancellationToken>()).Returns(true);
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns(followerId);

        var sut = new UnfollowUserCommandHandler(service, currentUser);
        await sut.Handle(new UnfollowUserCommand(followedId), CancellationToken.None);

        await service.Received(1).RemoveUserFollowAsync(followerId, followedId, Arg.Any<CancellationToken>());
    }

    // ── FollowPost ───────────────────────────────────────────────────────────

    [Fact]
    public async Task FollowPost_saves_new_follow()
    {
        var clock = MakeClock();
        var userId = System.Guid.NewGuid();
        var postId = System.Guid.NewGuid();

        var service = Substitute.For<ICommunityWriteService>();
        service.FindPostFollowAsync(postId, userId, Arg.Any<CancellationToken>())
            .Returns((PostFollow?)null);
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns(userId);

        var sut = new FollowPostCommandHandler(service, currentUser, clock);
        await sut.Handle(new FollowPostCommand(postId), CancellationToken.None);

        await service.Received(1).SaveFollowAsync(
            Arg.Is<PostFollow>(f => f.PostId == postId && f.UserId == userId),
            Arg.Any<CancellationToken>());
    }

    // ── UnfollowPost ─────────────────────────────────────────────────────────

    [Fact]
    public async Task UnfollowPost_calls_remove()
    {
        var userId = System.Guid.NewGuid();
        var postId = System.Guid.NewGuid();

        var service = Substitute.For<ICommunityWriteService>();
        service.RemovePostFollowAsync(postId, userId, Arg.Any<CancellationToken>()).Returns(true);
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns(userId);

        var sut = new UnfollowPostCommandHandler(service, currentUser);
        await sut.Handle(new UnfollowPostCommand(postId), CancellationToken.None);

        await service.Received(1).RemovePostFollowAsync(postId, userId, Arg.Any<CancellationToken>());
    }
}
