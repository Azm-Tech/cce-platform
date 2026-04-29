using CCE.Application.Content;
using CCE.Application.Content.Commands.CreatePage;
using CCE.Domain.Content;

namespace CCE.Application.Tests.Content.Commands;

public class CreatePageCommandHandlerTests
{
    [Fact]
    public async Task Persists_page_when_inputs_valid()
    {
        var (sut, service) = BuildSut();

        await sut.Handle(BuildCmd(), CancellationToken.None);

        await service.Received(1).SaveAsync(Arg.Any<Page>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Returns_dto_with_correct_fields()
    {
        var (sut, _) = BuildSut();

        var dto = await sut.Handle(BuildCmd(), CancellationToken.None);

        dto.Slug.Should().Be("test-slug");
        dto.PageType.Should().Be(PageType.Custom);
        dto.TitleAr.Should().Be("ar");
        dto.TitleEn.Should().Be("en");
        dto.ContentAr.Should().Be("content-ar");
        dto.ContentEn.Should().Be("content-en");
    }

    private static CreatePageCommand BuildCmd() =>
        new("test-slug", PageType.Custom, "ar", "en", "content-ar", "content-en");

    private static (CreatePageCommandHandler sut, IPageService service) BuildSut()
    {
        var service = Substitute.For<IPageService>();
        var sut = new CreatePageCommandHandler(service);
        return (sut, service);
    }
}
