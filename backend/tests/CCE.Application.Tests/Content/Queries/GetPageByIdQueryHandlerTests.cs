using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Queries.GetPageById;
using CCE.Domain.Content;

namespace CCE.Application.Tests.Content.Queries;

public class GetPageByIdQueryHandlerTests
{
    [Fact]
    public async Task Returns_null_when_page_not_found()
    {
        var db = BuildDb(Array.Empty<Page>());
        var sut = new GetPageByIdQueryHandler(db);

        var result = await sut.Handle(new GetPageByIdQuery(System.Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Returns_dto_with_all_fields_when_found()
    {
        var page = Page.Create("test-slug", PageType.Custom, "ar", "en", "content-ar", "content-en");

        var db = BuildDb([page]);
        var sut = new GetPageByIdQueryHandler(db);

        var result = await sut.Handle(new GetPageByIdQuery(page.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(page.Id);
        result.Slug.Should().Be("test-slug");
        result.PageType.Should().Be(PageType.Custom);
        result.TitleAr.Should().Be("ar");
        result.TitleEn.Should().Be("en");
        result.ContentAr.Should().Be("content-ar");
        result.ContentEn.Should().Be("content-en");
        result.RowVersion.Should().NotBeNull();
    }

    private static ICceDbContext BuildDb(IEnumerable<Page> pages)
    {
        var db = Substitute.For<ICceDbContext>();
        db.Pages.Returns(pages.AsQueryable());
        return db;
    }
}
