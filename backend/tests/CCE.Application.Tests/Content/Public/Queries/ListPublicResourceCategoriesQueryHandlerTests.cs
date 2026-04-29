using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Public.Queries.ListPublicResourceCategories;
using CCE.Domain.Content;

namespace CCE.Application.Tests.Content.Public.Queries;

public class ListPublicResourceCategoriesQueryHandlerTests
{
    [Fact]
    public async Task Returns_active_categories_sorted_by_order_index()
    {
        var cat1 = ResourceCategory.Create("تقارير", "Reports", "reports", null, 2);
        var cat2 = ResourceCategory.Create("أدلة", "Guides", "guides", null, 1);
        var inactive = ResourceCategory.Create("محفوظات", "Archives", "archives", null, 0);
        inactive.Deactivate();

        var db = BuildDb(new[] { cat1, cat2, inactive });
        var sut = new ListPublicResourceCategoriesQueryHandler(db);

        var result = await sut.Handle(new ListPublicResourceCategoriesQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].OrderIndex.Should().Be(1);
        result[0].NameEn.Should().Be("Guides");
        result[1].OrderIndex.Should().Be(2);
        result[1].NameEn.Should().Be("Reports");
    }

    [Fact]
    public async Task Returns_empty_when_no_active_categories_exist()
    {
        var inactive = ResourceCategory.Create("تقارير", "Reports", "reports", null, 1);
        inactive.Deactivate();

        var db = BuildDb(new[] { inactive });
        var sut = new ListPublicResourceCategoriesQueryHandler(db);

        var result = await sut.Handle(new ListPublicResourceCategoriesQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    private static ICceDbContext BuildDb(IEnumerable<ResourceCategory> categories)
    {
        var db = Substitute.For<ICceDbContext>();
        db.ResourceCategories.Returns(categories.AsQueryable());
        db.Users.Returns(System.Array.Empty<CCE.Domain.Identity.User>().AsQueryable());
        db.Roles.Returns(System.Array.Empty<CCE.Domain.Identity.Role>().AsQueryable());
        db.UserRoles.Returns(System.Array.Empty<Microsoft.AspNetCore.Identity.IdentityUserRole<System.Guid>>().AsQueryable());
        db.Resources.Returns(System.Array.Empty<CCE.Domain.Content.Resource>().AsQueryable());
        return db;
    }
}
