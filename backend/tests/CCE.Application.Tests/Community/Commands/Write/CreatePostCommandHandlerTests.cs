using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Sanitization;
using CCE.Application.Community;
using CCE.Application.Community.Commands.CreatePost;
using CCE.Domain.Common;
using CCE.Domain.Community;

namespace CCE.Application.Tests.Community.Commands.Write;

public class CreatePostCommandHandlerTests
{
    private static ISystemClock MakeClock()
    {
        var clock = Substitute.For<ISystemClock>();
        clock.UtcNow.Returns(System.DateTimeOffset.UtcNow);
        return clock;
    }

    [Fact]
    public async Task Creates_post_and_returns_id()
    {
        var clock = MakeClock();
        var authorId = System.Guid.NewGuid();
        var topicId = System.Guid.NewGuid();

        var service = Substitute.For<ICommunityWriteService>();
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns(authorId);
        var sanitizer = Substitute.For<IHtmlSanitizer>();
        sanitizer.Sanitize(Arg.Any<string>()).Returns(x => x.Arg<string>());

        var sut = new CreatePostCommandHandler(service, currentUser, sanitizer, clock);
        var cmd = new CreatePostCommand(topicId, "Hello world", "en", false);

        var id = await sut.Handle(cmd, CancellationToken.None);

        id.Should().NotBeEmpty();
        await service.Received(1).SavePostAsync(Arg.Is<Post>(p =>
            p.TopicId == topicId && p.AuthorId == authorId && p.Content == "Hello world"), Arg.Any<CancellationToken>());
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

        var sut = new CreatePostCommandHandler(service, currentUser, sanitizer, clock);

        var act = async () => await sut.Handle(
            new CreatePostCommand(System.Guid.NewGuid(), "content", "ar", false), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Sanitizes_content_before_factory()
    {
        var clock = MakeClock();
        var authorId = System.Guid.NewGuid();
        var service = Substitute.For<ICommunityWriteService>();
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns(authorId);
        var sanitizer = Substitute.For<IHtmlSanitizer>();
        sanitizer.Sanitize("<script>bad</script>clean").Returns("clean");

        var sut = new CreatePostCommandHandler(service, currentUser, sanitizer, clock);
        var cmd = new CreatePostCommand(System.Guid.NewGuid(), "<script>bad</script>clean", "en", false);

        await sut.Handle(cmd, CancellationToken.None);

        await service.Received(1).SavePostAsync(
            Arg.Is<Post>(p => p.Content == "clean"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_DomainException_when_sanitized_content_is_empty()
    {
        var clock = MakeClock();
        var authorId = System.Guid.NewGuid();
        var service = Substitute.For<ICommunityWriteService>();
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns(authorId);
        var sanitizer = Substitute.For<IHtmlSanitizer>();
        sanitizer.Sanitize(Arg.Any<string>()).Returns(string.Empty);

        var sut = new CreatePostCommandHandler(service, currentUser, sanitizer, clock);

        var act = async () => await sut.Handle(
            new CreatePostCommand(System.Guid.NewGuid(), "<b></b>", "ar", false), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }
}
