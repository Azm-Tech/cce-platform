using CCE.Application.Common.Interfaces;
using CCE.Application.Community;
using CCE.Application.Community.Commands.SoftDeletePost;
using CCE.Domain.Common;
using CCE.Domain.Community;

namespace CCE.Application.Tests.Community.Commands;

public class SoftDeletePostCommandHandlerTests
{
    [Fact]
    public async Task Throws_KeyNotFoundException_when_post_not_found()
    {
        var service = Substitute.For<ICommunityModerationService>();
        service.FindPostAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>()).Returns((Post?)null);
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        var clock = Substitute.For<ISystemClock>();
        var sut = new SoftDeletePostCommandHandler(service, currentUser, clock);

        var act = async () => await sut.Handle(new SoftDeletePostCommand(System.Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<System.Collections.Generic.KeyNotFoundException>();
    }

    [Fact]
    public async Task Throws_DomainException_when_no_actor()
    {
        var clock = Substitute.For<ISystemClock>();
        clock.UtcNow.Returns(System.DateTimeOffset.UtcNow);
        var post = Post.Create(System.Guid.NewGuid(), System.Guid.NewGuid(), "content", "ar", isAnswerable: false, clock);
        var service = Substitute.For<ICommunityModerationService>();
        service.FindPostAsync(post.Id, Arg.Any<CancellationToken>()).Returns(post);
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns((System.Guid?)null);
        var sut = new SoftDeletePostCommandHandler(service, currentUser, clock);

        var act = async () => await sut.Handle(new SoftDeletePostCommand(post.Id), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task SoftDeletes_post_and_calls_UpdatePostAsync()
    {
        var clock = Substitute.For<ISystemClock>();
        clock.UtcNow.Returns(System.DateTimeOffset.UtcNow);
        var post = Post.Create(System.Guid.NewGuid(), System.Guid.NewGuid(), "content", "ar", isAnswerable: false, clock);
        var moderatorId = System.Guid.NewGuid();
        var service = Substitute.For<ICommunityModerationService>();
        service.FindPostAsync(post.Id, Arg.Any<CancellationToken>()).Returns(post);
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns(moderatorId);
        var sut = new SoftDeletePostCommandHandler(service, currentUser, clock);

        await sut.Handle(new SoftDeletePostCommand(post.Id), CancellationToken.None);

        post.IsDeleted.Should().BeTrue();
        post.DeletedById.Should().Be(moderatorId);
        await service.Received(1).UpdatePostAsync(post, Arg.Any<CancellationToken>());
    }
}
