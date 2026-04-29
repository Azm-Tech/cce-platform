using CCE.Application.Common.Interfaces;
using CCE.Application.Community;
using CCE.Application.Community.Commands.DeleteTopic;
using CCE.Domain.Common;
using CCE.Domain.Community;

namespace CCE.Application.Tests.Community.Commands;

public class DeleteTopicCommandHandlerTests
{
    [Fact]
    public async Task Throws_KeyNotFoundException_when_topic_not_found()
    {
        var service = Substitute.For<ITopicService>();
        service.FindAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>()).Returns((Topic?)null);
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        var clock = Substitute.For<ISystemClock>();
        var sut = new DeleteTopicCommandHandler(service, currentUser, clock);

        var act = async () => await sut.Handle(new DeleteTopicCommand(System.Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<System.Collections.Generic.KeyNotFoundException>();
    }

    [Fact]
    public async Task Throws_DomainException_when_no_actor()
    {
        var topic = Topic.Create("طاقة", "Energy", "وصف", "Description", "energy", null, null, 0);
        var service = Substitute.For<ITopicService>();
        service.FindAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns((System.Guid?)null);
        var clock = Substitute.For<ISystemClock>();
        var sut = new DeleteTopicCommandHandler(service, currentUser, clock);

        var act = async () => await sut.Handle(new DeleteTopicCommand(topic.Id), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task SoftDeletes_topic_and_calls_UpdateAsync()
    {
        var topic = Topic.Create("طاقة", "Energy", "وصف", "Description", "energy", null, null, 0);
        var adminId = System.Guid.NewGuid();
        var service = Substitute.For<ITopicService>();
        service.FindAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns(adminId);
        var clock = Substitute.For<ISystemClock>();
        clock.UtcNow.Returns(System.DateTimeOffset.UtcNow);
        var sut = new DeleteTopicCommandHandler(service, currentUser, clock);

        await sut.Handle(new DeleteTopicCommand(topic.Id), CancellationToken.None);

        topic.IsDeleted.Should().BeTrue();
        topic.DeletedById.Should().Be(adminId);
        await service.Received(1).UpdateAsync(topic, Arg.Any<CancellationToken>());
    }
}
