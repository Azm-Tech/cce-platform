using CCE.Application.Common.Interfaces;
using CCE.Application.Community;
using CCE.Application.Community.Commands.MarkPostAnswered;
using CCE.Domain.Common;
using CCE.Domain.Community;

namespace CCE.Application.Tests.Community.Commands.Write;

public class MarkPostAnsweredCommandHandlerTests
{
    private static ISystemClock MakeClock()
    {
        var clock = Substitute.For<ISystemClock>();
        clock.UtcNow.Returns(System.DateTimeOffset.UtcNow);
        return clock;
    }

    [Fact]
    public async Task Marks_post_answered_when_author_calls()
    {
        var clock = MakeClock();
        var authorId = System.Guid.NewGuid();
        var post = Post.Create(System.Guid.NewGuid(), authorId, "Question?", "en", isAnswerable: true, clock);
        var reply = PostReply.Create(post.Id, System.Guid.NewGuid(), "Answer!", "en", null, false, clock);

        var service = Substitute.For<ICommunityWriteService>();
        service.FindPostAsync(post.Id, Arg.Any<CancellationToken>()).Returns(post);
        service.FindReplyAsync(reply.Id, Arg.Any<CancellationToken>()).Returns(reply);
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns(authorId);

        var sut = new MarkPostAnsweredCommandHandler(service, currentUser);

        await sut.Handle(new MarkPostAnsweredCommand(post.Id, reply.Id), CancellationToken.None);

        post.AnsweredReplyId.Should().Be(reply.Id);
        await service.Received(1).UpdatePostAsync(post, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_UnauthorizedAccessException_when_not_author()
    {
        var clock = MakeClock();
        var post = Post.Create(System.Guid.NewGuid(), System.Guid.NewGuid(), "Question?", "en", isAnswerable: true, clock);
        var reply = PostReply.Create(post.Id, System.Guid.NewGuid(), "Answer!", "en", null, false, clock);

        var service = Substitute.For<ICommunityWriteService>();
        service.FindPostAsync(post.Id, Arg.Any<CancellationToken>()).Returns(post);
        service.FindReplyAsync(reply.Id, Arg.Any<CancellationToken>()).Returns(reply);
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns(System.Guid.NewGuid()); // different user

        var sut = new MarkPostAnsweredCommandHandler(service, currentUser);

        var act = async () => await sut.Handle(
            new MarkPostAnsweredCommand(post.Id, reply.Id), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Throws_KeyNotFoundException_when_post_missing()
    {
        var service = Substitute.For<ICommunityWriteService>();
        service.FindPostAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>()).Returns((Post?)null);
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns(System.Guid.NewGuid());

        var sut = new MarkPostAnsweredCommandHandler(service, currentUser);

        var act = async () => await sut.Handle(
            new MarkPostAnsweredCommand(System.Guid.NewGuid(), System.Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Throws_DomainException_when_post_not_answerable()
    {
        var clock = MakeClock();
        var authorId = System.Guid.NewGuid();
        var post = Post.Create(System.Guid.NewGuid(), authorId, "Discussion", "en", isAnswerable: false, clock);
        var reply = PostReply.Create(post.Id, System.Guid.NewGuid(), "Reply", "en", null, false, clock);

        var service = Substitute.For<ICommunityWriteService>();
        service.FindPostAsync(post.Id, Arg.Any<CancellationToken>()).Returns(post);
        service.FindReplyAsync(reply.Id, Arg.Any<CancellationToken>()).Returns(reply);
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns(authorId);

        var sut = new MarkPostAnsweredCommandHandler(service, currentUser);

        var act = async () => await sut.Handle(
            new MarkPostAnsweredCommand(post.Id, reply.Id), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }
}
