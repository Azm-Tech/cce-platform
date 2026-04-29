using CCE.Application.Community;
using CCE.Application.Community.Commands.UpdateTopic;
using CCE.Domain.Community;

namespace CCE.Application.Tests.Community.Commands;

public class UpdateTopicCommandHandlerTests
{
    [Fact]
    public async Task Returns_null_when_topic_not_found()
    {
        var service = Substitute.For<ITopicService>();
        service.FindAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>()).Returns((Topic?)null);
        var sut = new UpdateTopicCommandHandler(service);

        var result = await sut.Handle(BuildCommand(System.Guid.NewGuid(), isActive: true), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Updates_content_reorders_and_calls_UpdateAsync()
    {
        var topic = Topic.Create("قديم", "Old", "وصف قديم", "Old description", "old-slug", null, null, 1);
        var service = Substitute.For<ITopicService>();
        service.FindAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        var sut = new UpdateTopicCommandHandler(service);

        var cmd = new UpdateTopicCommand(topic.Id, "جديد", "New", "وصف جديد", "New description", 10, true);
        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result!.NameAr.Should().Be("جديد");
        result.NameEn.Should().Be("New");
        result.OrderIndex.Should().Be(10);
        result.IsActive.Should().BeTrue();
        await service.Received(1).UpdateAsync(topic, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deactivates_when_IsActive_is_false()
    {
        var topic = Topic.Create("نشط", "Active", "وصف", "Description", "active-topic", null, null, 0);
        var service = Substitute.For<ITopicService>();
        service.FindAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        var sut = new UpdateTopicCommandHandler(service);

        var cmd = new UpdateTopicCommand(topic.Id, "نشط", "Active", "وصف", "Description", 0, false);
        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result!.IsActive.Should().BeFalse();
        topic.IsActive.Should().BeFalse();
    }

    private static UpdateTopicCommand BuildCommand(System.Guid id, bool isActive) =>
        new(id, "اسم عربي", "English Name", "وصف عربي", "English description", 0, isActive);
}
