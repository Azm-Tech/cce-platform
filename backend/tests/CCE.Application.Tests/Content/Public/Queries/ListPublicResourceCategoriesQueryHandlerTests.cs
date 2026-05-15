using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Public.Queries.ListPublicResourceCategories;
using CCE.Domain.Content;

namespace CCE.Application.Tests.Content.Public.Queries;

public class ListPublicResourceCategoriesQueryHandlerTests
{
    [Fact]
    public async Task Returns_active_categories_sorted_by_order_index()
    {
        var guides = ResourceCategory.Create("أدلة", "Guides", "guides", null, 2);
        var reports = ResourceCategory.Create("تقارير", "Reports", "reports", null, 1);

        var db = BuildDb([guides, reports]);
        var sut = new ListPublicResourceCategoriesQueryHandler(db);

        var result = await sut.Handle(new ListPublicResourceCategoriesQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].OrderIndex.Should().Be(1);
        result[0].NameEn.Should().Be("Reports");
        result[1].OrderIndex.Should().Be(2);
        result[1].NameEn.Should().Be("Guides");
    }

    [Fact]
    public async Task Returns_empty_when_no_active_categories_exist()
    {
        var inactive = ResourceCategory.Create("غير نشط", "Inactive", "inactive", null, 1);
        inactive.Deactivate();

        var db = BuildDb([inactive]);
        var sut = new ListPublicResourceCategoriesQueryHandler(db);

        var result = await sut.Handle(new ListPublicResourceCategoriesQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Excludes_inactive_categories()
    {
        var active = ResourceCategory.Create("نشط", "Active", "active", null, 1);
        var inactive = ResourceCategory.Create("غير نشط", "Inactive", "inactive", null, 2);
        inactive.Deactivate();

        var db = BuildDb([active, inactive]);
        var sut = new ListPublicResourceCategoriesQueryHandler(db);

        var result = await sut.Handle(new ListPublicResourceCategoriesQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].NameEn.Should().Be("Active");
    }

    private static ICceDbContext BuildDb(IEnumerable<ResourceCategory> categories)
    {
        var db = Substitute.For<ICceDbContext>();
        db.ResourceCategories.Returns(categories.AsQueryable());
        return db;
    }
}
