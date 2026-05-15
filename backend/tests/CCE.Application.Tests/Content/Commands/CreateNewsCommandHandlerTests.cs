using CCE.Application.Common.Interfaces;
using CCE.Application.Content;
using CCE.Application.Content.Commands.CreateNews;
using CCE.Domain.Common;
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Content.Commands;

public class CreateNewsCommandHandlerTests
{
    [Fact]
    public async Task Throws_DomainException_when_actor_unknown()
    {
        var (sut, _, _) = BuildSut(noUser: true);

        var act = async () => await sut.Handle(BuildCmd(), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Persists_news_when_inputs_valid()
    {
        var (sut, service, _) = BuildSut();

        await sut.Handle(BuildCmd(), CancellationToken.None);

        await service.Received(1).SaveAsync(Arg.Any<News>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Returns_dto_with_correct_fields()
    {
        var (sut, _, _) = BuildSut();

        var dto = await sut.Handle(BuildCmd(), CancellationToken.None);

        dto.TitleAr.Should().Be("خبر");
        dto.TitleEn.Should().Be("News");
        dto.Slug.Should().Be("first-post");
        dto.IsPublished.Should().BeFalse();
    }

    private static CreateNewsCommand BuildCmd() =>
        new("خبر", "News", "محتوى", "Content", "first-post", null);

    private static (CreateNewsCommandHandler sut, INewsRepository service, ICurrentUserAccessor user) BuildSut(bool noUser = false)
    {
        var service = Substitute.For<INewsRepository>();
        var user = Substitute.For<ICurrentUserAccessor>();
        if (noUser)
            user.GetUserId().Returns((System.Guid?)null);
        else
            user.GetUserId().Returns(System.Guid.NewGuid());
        var sut = new CreateNewsCommandHandler(service, user, new FakeSystemClock());
        return (sut, service, user);
    }
}
