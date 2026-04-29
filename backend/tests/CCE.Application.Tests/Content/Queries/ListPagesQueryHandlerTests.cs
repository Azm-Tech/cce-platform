using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Queries.ListPages;
using CCE.Domain.Content;

namespace CCE.Application.Tests.Content.Queries;

public class ListPagesQueryHandlerTests
{
    [Fact]
    public async Task Returns_empty_paged_result_when_no_pages_exist()
    {
        var db = BuildDb(System.Array.Empty<Page>());
        var sut = new ListPagesQueryHandler(db);

        var result = await sut.Handle(new ListPagesQuery(Page: 1, PageSize: 20), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.Total.Should().Be(0);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task Returns_pages_sorted_by_Slug_ascending()
    {
        var alpha = Page.Create("alpha-page", PageType.Custom, "أ", "Alpha", "محتوى", "content");
        var beta = Page.Create("beta-page", PageType.Custom, "ب", "Beta", "محتوى", "content");

        var db = BuildDb(new[] { beta, alpha });
        var sut = new ListPagesQueryHandler(db);

        var result = await sut.Handle(new ListPagesQuery(Page: 1, PageSize: 20), CancellationToken.None);

        result.Total.Should().Be(2);
        result.Items[0].Slug.Should().Be("alpha-page");
        result.Items[1].Slug.Should().Be("beta-page");
    }

    [Fact]
    public async Task Search_filter_matches_slug_titleAr_or_titleEn()
    {
        var match = Page.Create("test-slug", PageType.Custom, "ar", "matching-title", "content-ar", "content-en");
        var noMatch = Page.Create("other-slug", PageType.Custom, "ar", "other-title", "content-ar", "content-en");

        var db = BuildDb(new[] { match, noMatch });
        var sut = new ListPagesQueryHandler(db);

        var result = await sut.Handle(new ListPagesQuery(Search: "matching"), CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items.Single().TitleEn.Should().Be("matching-title");
    }

    private static ICceDbContext BuildDb(IEnumerable<Page> pages)
    {
        var db = Substitute.For<ICceDbContext>();
        db.Pages.Returns(pages.AsQueryable());
        return db;
    }
}
