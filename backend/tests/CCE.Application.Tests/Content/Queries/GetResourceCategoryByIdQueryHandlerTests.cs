using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Queries.GetResourceCategoryById;
using CCE.Domain.Content;

namespace CCE.Application.Tests.Content.Queries;

public class GetResourceCategoryByIdQueryHandlerTests
{
    [Fact]
    public async Task Returns_null_when_category_not_found()
    {
        var db = BuildDb(System.Array.Empty<ResourceCategory>());
        var sut = new GetResourceCategoryByIdQueryHandler(db);

        var result = await sut.Handle(new GetResourceCategoryByIdQuery(System.Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Returns_dto_with_all_fields_when_found()
    {
        var category = ResourceCategory.Create("تقنية", "Technology", "technology", null, 5);

        var db = BuildDb(new[] { category });
        var sut = new GetResourceCategoryByIdQueryHandler(db);

        var result = await sut.Handle(new GetResourceCategoryByIdQuery(category.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(category.Id);
        result.NameAr.Should().Be("تقنية");
        result.NameEn.Should().Be("Technology");
        result.Slug.Should().Be("technology");
        result.ParentId.Should().BeNull();
        result.OrderIndex.Should().Be(5);
        result.IsActive.Should().BeTrue();
    }

    private static ICceDbContext BuildDb(IEnumerable<ResourceCategory> categories)
    {
        var db = Substitute.For<ICceDbContext>();
        db.ResourceCategories.Returns(categories.AsQueryable());
        return db;
    }
}
