using CCE.Application.Common.Interfaces;
using CCE.Application.Content;
using CCE.Application.Content.Commands.DeleteNews;
using CCE.Domain.Common;
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Content.Commands;

public class DeleteNewsCommandHandlerTests
{
    [Fact]
    public async Task Throws_KeyNotFound_when_news_missing()
    {
        var service = Substitute.For<INewsRepository>();
        service.FindAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>()).Returns((News?)null);
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        var sut = new DeleteNewsCommandHandler(service, currentUser, new FakeSystemClock());

        var act = async () => await sut.Handle(new DeleteNewsCommand(System.Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<System.Collections.Generic.KeyNotFoundException>();
    }

    [Fact]
    public async Task Throws_DomainException_when_actor_unknown()
    {
        var clock = new FakeSystemClock();
        var news = News.Draft("ar", "en", "content-ar", "content-en", "slug", System.Guid.NewGuid(), null, clock);

        var service = Substitute.For<INewsRepository>();
        service.FindAsync(news.Id, Arg.Any<CancellationToken>()).Returns(news);

        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns((System.Guid?)null);

        var sut = new DeleteNewsCommandHandler(service, currentUser, clock);

        var act = async () => await sut.Handle(new DeleteNewsCommand(news.Id), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Soft_deletes_and_calls_UpdateAsync()
    {
        var clock = new FakeSystemClock();
        var actorId = System.Guid.NewGuid();
        var news = News.Draft("ar", "en", "content-ar", "content-en", "slug", System.Guid.NewGuid(), null, clock);

        var service = Substitute.For<INewsRepository>();
        service.FindAsync(news.Id, Arg.Any<CancellationToken>()).Returns(news);

        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns(actorId);

        var sut = new DeleteNewsCommandHandler(service, currentUser, clock);

        await sut.Handle(new DeleteNewsCommand(news.Id), CancellationToken.None);

        news.IsDeleted.Should().BeTrue();
        await service.Received(1).UpdateAsync(news, Arg.Any<byte[]>(), Arg.Any<CancellationToken>());
    }
}
