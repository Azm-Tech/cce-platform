using CCE.Application.Community;
using CCE.Application.Community.Commands.CreateTopic;
using CCE.Domain.Community;

namespace CCE.Application.Tests.Community.Commands;

public class CreateTopicCommandHandlerTests
{
    [Fact]
    public async Task Creates_topic_saves_and_returns_dto()
    {
        var service = Substitute.For<ITopicService>();
        var sut = new CreateTopicCommandHandler(service);

        var cmd = new CreateTopicCommand("طاقة", "Energy", "وصف الطاقة", "Energy description", "energy", null, null, 0);
        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.NameAr.Should().Be("طاقة");
        result.NameEn.Should().Be("Energy");
        result.Slug.Should().Be("energy");
        result.IsActive.Should().BeTrue();
        await service.Received(1).SaveAsync(Arg.Any<Topic>(), Arg.Any<CancellationToken>());
    }
}
