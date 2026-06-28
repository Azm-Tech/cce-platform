using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Queries.ListResourceCategories;
using CCE.Domain.Content;

namespace CCE.Application.Tests.Content.Queries;

public class ListResourceCategoriesQueryHandlerTests
{
    [Fact]
    public async Task Returns_empty_paged_result_when_no_categories_exist()
    {
        var db = BuildDb(Array.Empty<ResourceCategory>());
        var sut = new ListResourceCategoriesQueryHandler(db);

        var result = await sut.Handle(new ListResourceCategoriesQuery(Page: 1, PageSize: 20), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.Total.Should().Be(0);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task IsActive_filter_returns_only_active_categories()
    {
        var active = ResourceCategory.Create("نشط", "Active", "active", null, 1);
        var inactive = ResourceCategory.Create("غير نشط", "Inactive", "inactive", null, 2);
        inactive.Deactivate();

        var db = BuildDb([active, inactive]);
        var sut = new ListResourceCategoriesQueryHandler(db);

        var result = await sut.Handle(new ListResourceCategoriesQuery(IsActive: true), CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items.Single().NameEn.Should().Be("Active");
    }

    [Fact]
    public async Task ParentId_filter_returns_only_children_of_given_parent()
    {
        var parentId = System.Guid.NewGuid();
        var child = ResourceCategory.Create("فرعي", "Child", "child", parentId, 1);
        var unrelated = ResourceCategory.Create("مستقل", "Standalone", "standalone", null, 2);

        var db = BuildDb([child, unrelated]);
        var sut = new ListResourceCategoriesQueryHandler(db);

        var result = await sut.Handle(new ListResourceCategoriesQuery(ParentId: parentId), CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items.Single().NameEn.Should().Be("Child");
    }

    [Fact]
    public async Task Returns_categories_sorted_by_OrderIndex()
    {
        var second = ResourceCategory.Create("ثاني", "Second", "second", null, 5);
        var first = ResourceCategory.Create("أول", "First", "first", null, 1);

        var db = BuildDb([second, first]);
        var sut = new ListResourceCategoriesQueryHandler(db);

        var result = await sut.Handle(new ListResourceCategoriesQuery(), CancellationToken.None);

        result.Total.Should().Be(2);
        result.Items[0].NameEn.Should().Be("First");
        result.Items[1].NameEn.Should().Be("Second");
    }

    private static ICceDbContext BuildDb(IEnumerable<ResourceCategory> categories)
    {
        var db = Substitute.For<ICceDbContext>();
        db.ResourceCategories.Returns(categories.AsQueryable());
        return db;
    }
}
