using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Queries.ListResourceCategories;
using CCE.Domain.Content;

namespace CCE.Application.Tests.Content.Queries;

public class ListResourceCategoriesQueryHandlerTests
{
    [Fact]
    public async Task Returns_empty_paged_result_when_no_categories_exist()
    {
        var db = BuildDb(System.Array.Empty<ResourceCategory>());
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

        var db = BuildDb(new[] { active, inactive });
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
        var root = ResourceCategory.Create("جذر", "Root", "root", null, 0);

        var db = BuildDb(new[] { child, root });
        var sut = new ListResourceCategoriesQueryHandler(db);

        var result = await sut.Handle(new ListResourceCategoriesQuery(ParentId: parentId), CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items.Single().NameEn.Should().Be("Child");
    }

    private static ICceDbContext BuildDb(IEnumerable<ResourceCategory> categories)
    {
        var db = Substitute.For<ICceDbContext>();
        db.ResourceCategories.Returns(categories.AsQueryable());
        return db;
    }
}
