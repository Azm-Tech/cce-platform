using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Sanitization;
using CCE.Application.Community;
using CCE.Application.Community.Commands.CreateReply;
using CCE.Domain.Common;
using CCE.Domain.Community;

namespace CCE.Application.Tests.Community.Commands.Write;

public class CreateReplyCommandHandlerTests
{
    private static ISystemClock MakeClock()
    {
        var clock = Substitute.For<ISystemClock>();
        clock.UtcNow.Returns(System.DateTimeOffset.UtcNow);
        return clock;
    }

    private static Post MakePost(ISystemClock clock)
        => Post.Create(System.Guid.NewGuid(), System.Guid.NewGuid(), "Post content", "en", false, clock);

    [Fact]
    public async Task Creates_reply_and_returns_id()
    {
        var clock = MakeClock();
        var post = MakePost(clock);
        var authorId = System.Guid.NewGuid();

        var service = Substitute.For<ICommunityWriteService>();
        service.FindPostAsync(post.Id, Arg.Any<CancellationToken>()).Returns(post);
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns(authorId);
        var sanitizer = Substitute.For<IHtmlSanitizer>();
        sanitizer.Sanitize(Arg.Any<string>()).Returns(x => x.Arg<string>());

        var sut = new CreateReplyCommandHandler(service, currentUser, sanitizer, clock);
        var cmd = new CreateReplyCommand(post.Id, "My reply", "en", null);

        var id = await sut.Handle(cmd, CancellationToken.None);

        id.Should().NotBeEmpty();
        await service.Received(1).SaveReplyAsync(
            Arg.Is<PostReply>(r => r.PostId == post.Id && r.AuthorId == authorId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_KeyNotFoundException_when_post_missing()
    {
        var clock = MakeClock();
        var service = Substitute.For<ICommunityWriteService>();
        service.FindPostAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>()).Returns((Post?)null);
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns(System.Guid.NewGuid());
        var sanitizer = Substitute.For<IHtmlSanitizer>();
        sanitizer.Sanitize(Arg.Any<string>()).Returns(x => x.Arg<string>());

        var sut = new CreateReplyCommandHandler(service, currentUser, sanitizer, clock);

        var act = async () => await sut.Handle(
            new CreateReplyCommand(System.Guid.NewGuid(), "reply", "ar", null), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Throws_DomainException_when_no_user()
    {
        var clock = MakeClock();
        var service = Substitute.For<ICommunityWriteService>();
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns((System.Guid?)null);
        var sanitizer = Substitute.For<IHtmlSanitizer>();
        sanitizer.Sanitize(Arg.Any<string>()).Returns(x => x.Arg<string>());

        var sut = new CreateReplyCommandHandler(service, currentUser, sanitizer, clock);

        var act = async () => await sut.Handle(
            new CreateReplyCommand(System.Guid.NewGuid(), "reply", "ar", null), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }
}
