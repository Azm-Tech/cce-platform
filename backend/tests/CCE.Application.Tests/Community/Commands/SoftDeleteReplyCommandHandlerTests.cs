using CCE.Application.Common.Interfaces;
using CCE.Application.Community;
using CCE.Application.Community.Commands.SoftDeleteReply;
using CCE.Domain.Common;
using CCE.Domain.Community;

namespace CCE.Application.Tests.Community.Commands;

public class SoftDeleteReplyCommandHandlerTests
{
    [Fact]
    public async Task Throws_KeyNotFoundException_when_reply_not_found()
    {
        var service = Substitute.For<ICommunityModerationService>();
        service.FindReplyAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>()).Returns((PostReply?)null);
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        var clock = Substitute.For<ISystemClock>();
        var sut = new SoftDeleteReplyCommandHandler(service, currentUser, clock);

        var act = async () => await sut.Handle(new SoftDeleteReplyCommand(System.Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<System.Collections.Generic.KeyNotFoundException>();
    }

    [Fact]
    public async Task Throws_DomainException_when_no_actor()
    {
        var clock = Substitute.For<ISystemClock>();
        clock.UtcNow.Returns(System.DateTimeOffset.UtcNow);
        var reply = PostReply.Create(System.Guid.NewGuid(), System.Guid.NewGuid(), "content", "ar", parentReplyId: null, isByExpert: false, clock);
        var service = Substitute.For<ICommunityModerationService>();
        service.FindReplyAsync(reply.Id, Arg.Any<CancellationToken>()).Returns(reply);
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns((System.Guid?)null);
        var sut = new SoftDeleteReplyCommandHandler(service, currentUser, clock);

        var act = async () => await sut.Handle(new SoftDeleteReplyCommand(reply.Id), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task SoftDeletes_reply_and_calls_UpdateReplyAsync()
    {
        var clock = Substitute.For<ISystemClock>();
        clock.UtcNow.Returns(System.DateTimeOffset.UtcNow);
        var reply = PostReply.Create(System.Guid.NewGuid(), System.Guid.NewGuid(), "content", "ar", parentReplyId: null, isByExpert: false, clock);
        var moderatorId = System.Guid.NewGuid();
        var service = Substitute.For<ICommunityModerationService>();
        service.FindReplyAsync(reply.Id, Arg.Any<CancellationToken>()).Returns(reply);
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns(moderatorId);
        var sut = new SoftDeleteReplyCommandHandler(service, currentUser, clock);

        await sut.Handle(new SoftDeleteReplyCommand(reply.Id), CancellationToken.None);

        reply.IsDeleted.Should().BeTrue();
        reply.DeletedById.Should().Be(moderatorId);
        await service.Received(1).UpdateReplyAsync(reply, Arg.Any<CancellationToken>());
    }
}
