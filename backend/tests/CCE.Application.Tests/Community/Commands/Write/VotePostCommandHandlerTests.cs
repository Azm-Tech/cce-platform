using CCE.Application.Common.Interfaces;
using CCE.Application.Community;
using CCE.Application.Community.Commands.VotePost;
using CCE.Application.Localization;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Community;
using Microsoft.Extensions.Logging.Abstractions;

namespace CCE.Application.Tests.Community.Commands.Write;

public class VotePostCommandHandlerTests
{
    private static ISystemClock MakeClock()
    {
        var clock = Substitute.For<ISystemClock>();
        clock.UtcNow.Returns(System.DateTimeOffset.UtcNow);
        return clock;
    }

    private static MessageFactory MakeMsg()
        => new(Substitute.For<ILocalizationService>(), NullLogger<MessageFactory>.Instance);

    private static Post MakePost(ISystemClock clock)
        => Post.Create(System.Guid.NewGuid(), System.Guid.NewGuid(), "Content", "en", false, clock);

    [Fact]
    public async Task Upvote_adds_vote_and_increments_count()
    {
        var clock = MakeClock();
        var post = MakePost(clock);
        var userId = System.Guid.NewGuid();

        var repo = Substitute.For<ICommunityVoteRepository>();
        repo.GetPostAsync(post.Id, Arg.Any<CancellationToken>()).Returns(post);
        repo.FindPostVoteAsync(post.Id, userId, Arg.Any<CancellationToken>()).Returns((PostVote?)null);
        var db = Substitute.For<ICceDbContext>();
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns(userId);

        var sut = new VotePostCommandHandler(repo, db, currentUser, clock, MakeMsg());

        var result = await sut.Handle(new VotePostCommand(post.Id, VoteDirection.Up), CancellationToken.None);

        result.Success.Should().BeTrue();
        post.UpvoteCount.Should().Be(1);
        post.DownvoteCount.Should().Be(0);
        repo.Received(1).AddPostVote(Arg.Is<PostVote>(v => v.PostId == post.Id && v.UserId == userId && v.Value == 1));
        await db.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Flipping_up_to_down_moves_the_count()
    {
        var clock = MakeClock();
        var post = MakePost(clock);
        var userId = System.Guid.NewGuid();
        post.ApplyVote(0, 1); // pre-existing upvote reflected on the aggregate
        var existing = PostVote.Cast(post.Id, userId, 1, clock);

        var repo = Substitute.For<ICommunityVoteRepository>();
        repo.GetPostAsync(post.Id, Arg.Any<CancellationToken>()).Returns(post);
        repo.FindPostVoteAsync(post.Id, userId, Arg.Any<CancellationToken>()).Returns(existing);
        var db = Substitute.For<ICceDbContext>();
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns(userId);

        var sut = new VotePostCommandHandler(repo, db, currentUser, clock, MakeMsg());

        var result = await sut.Handle(new VotePostCommand(post.Id, VoteDirection.Down), CancellationToken.None);

        result.Success.Should().BeTrue();
        post.UpvoteCount.Should().Be(0);
        post.DownvoteCount.Should().Be(1);
        existing.Value.Should().Be(-1);
    }

    [Fact]
    public async Task None_retracts_existing_vote()
    {
        var clock = MakeClock();
        var post = MakePost(clock);
        var userId = System.Guid.NewGuid();
        post.ApplyVote(0, 1);
        var existing = PostVote.Cast(post.Id, userId, 1, clock);

        var repo = Substitute.For<ICommunityVoteRepository>();
        repo.GetPostAsync(post.Id, Arg.Any<CancellationToken>()).Returns(post);
        repo.FindPostVoteAsync(post.Id, userId, Arg.Any<CancellationToken>()).Returns(existing);
        var db = Substitute.For<ICceDbContext>();
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns(userId);

        var sut = new VotePostCommandHandler(repo, db, currentUser, clock, MakeMsg());

        var result = await sut.Handle(new VotePostCommand(post.Id, VoteDirection.None), CancellationToken.None);

        result.Success.Should().BeTrue();
        post.UpvoteCount.Should().Be(0);
        repo.Received(1).RemovePostVote(existing);
    }

    [Fact]
    public async Task Returns_not_found_when_post_missing()
    {
        var clock = MakeClock();
        var repo = Substitute.For<ICommunityVoteRepository>();
        repo.GetPostAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>()).Returns((Post?)null);
        var db = Substitute.For<ICceDbContext>();
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns(System.Guid.NewGuid());

        var sut = new VotePostCommandHandler(repo, db, currentUser, clock, MakeMsg());

        var result = await sut.Handle(new VotePostCommand(System.Guid.NewGuid(), VoteDirection.Up), CancellationToken.None);

        result.Success.Should().BeFalse();
        await db.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Returns_unauthorized_when_no_user()
    {
        var clock = MakeClock();
        var repo = Substitute.For<ICommunityVoteRepository>();
        var db = Substitute.For<ICceDbContext>();
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns((System.Guid?)null);

        var sut = new VotePostCommandHandler(repo, db, currentUser, clock, MakeMsg());

        var result = await sut.Handle(new VotePostCommand(System.Guid.NewGuid(), VoteDirection.Up), CancellationToken.None);

        result.Success.Should().BeFalse();
        await repo.DidNotReceive().GetPostAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>());
    }
}
