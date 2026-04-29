using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Sanitization;
using CCE.Application.Community;
using CCE.Application.Community.Commands.EditReply;
using CCE.Domain.Common;
using CCE.Domain.Community;

namespace CCE.Application.Tests.Community.Commands.Write;

public class EditReplyCommandHandlerTests
{
    private static ISystemClock MakeClockAt(System.DateTimeOffset now)
    {
        var clock = Substitute.For<ISystemClock>();
        clock.UtcNow.Returns(now);
        return clock;
    }

    [Fact]
    public async Task Edits_reply_within_window()
    {
        var createdAt = System.DateTimeOffset.UtcNow;
        var clock = MakeClockAt(createdAt);
        var authorId = System.Guid.NewGuid();
        var reply = PostReply.Create(System.Guid.NewGuid(), authorId, "original", "en", null, false, clock);

        // 5 minutes later — within window
        var editClock = MakeClockAt(createdAt.AddMinutes(5));
        var service = Substitute.For<ICommunityWriteService>();
        service.FindReplyAsync(reply.Id, Arg.Any<CancellationToken>()).Returns(reply);
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns(authorId);
        var sanitizer = Substitute.For<IHtmlSanitizer>();
        sanitizer.Sanitize(Arg.Any<string>()).Returns(x => x.Arg<string>());

        var sut = new EditReplyCommandHandler(service, currentUser, sanitizer, editClock);

        await sut.Handle(new EditReplyCommand(reply.Id, "updated content"), CancellationToken.None);

        reply.Content.Should().Be("updated content");
        await service.Received(1).UpdateReplyAsync(reply, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_DomainException_after_edit_window()
    {
        var createdAt = System.DateTimeOffset.UtcNow;
        var clock = MakeClockAt(createdAt);
        var authorId = System.Guid.NewGuid();
        var reply = PostReply.Create(System.Guid.NewGuid(), authorId, "original", "en", null, false, clock);

        // 20 minutes later — past the 15-min window
        var editClock = MakeClockAt(createdAt.AddMinutes(20));
        var service = Substitute.For<ICommunityWriteService>();
        service.FindReplyAsync(reply.Id, Arg.Any<CancellationToken>()).Returns(reply);
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns(authorId);
        var sanitizer = Substitute.For<IHtmlSanitizer>();
        sanitizer.Sanitize(Arg.Any<string>()).Returns(x => x.Arg<string>());

        var sut = new EditReplyCommandHandler(service, currentUser, sanitizer, editClock);

        var act = async () => await sut.Handle(
            new EditReplyCommand(reply.Id, "updated"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*15 minutes*");
    }

    [Fact]
    public async Task Throws_UnauthorizedAccessException_when_not_author()
    {
        var clock = MakeClockAt(System.DateTimeOffset.UtcNow);
        var reply = PostReply.Create(System.Guid.NewGuid(), System.Guid.NewGuid(), "original", "en", null, false, clock);

        var service = Substitute.For<ICommunityWriteService>();
        service.FindReplyAsync(reply.Id, Arg.Any<CancellationToken>()).Returns(reply);
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns(System.Guid.NewGuid()); // different user
        var sanitizer = Substitute.For<IHtmlSanitizer>();
        sanitizer.Sanitize(Arg.Any<string>()).Returns(x => x.Arg<string>());

        var sut = new EditReplyCommandHandler(service, currentUser, sanitizer, clock);

        var act = async () => await sut.Handle(
            new EditReplyCommand(reply.Id, "updated"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Throws_KeyNotFoundException_when_reply_missing()
    {
        var clock = MakeClockAt(System.DateTimeOffset.UtcNow);
        var service = Substitute.For<ICommunityWriteService>();
        service.FindReplyAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>()).Returns((PostReply?)null);
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns(System.Guid.NewGuid());
        var sanitizer = Substitute.For<IHtmlSanitizer>();
        sanitizer.Sanitize(Arg.Any<string>()).Returns(x => x.Arg<string>());

        var sut = new EditReplyCommandHandler(service, currentUser, sanitizer, clock);

        var act = async () => await sut.Handle(
            new EditReplyCommand(System.Guid.NewGuid(), "updated"), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
