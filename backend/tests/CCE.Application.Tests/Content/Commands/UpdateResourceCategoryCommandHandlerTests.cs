using CCE.Application.Content;
using CCE.Application.Content.Commands.UpdateResourceCategory;
using CCE.Domain.Content;

namespace CCE.Application.Tests.Content.Commands;

public class UpdateResourceCategoryCommandHandlerTests
{
    [Fact]
    public async Task Returns_null_when_category_not_found()
    {
        var service = Substitute.For<IResourceCategoryRepository>();
        service.FindAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>()).Returns((ResourceCategory?)null);
        var sut = new UpdateResourceCategoryCommandHandler(service);

        var result = await sut.Handle(BuildCommand(System.Guid.NewGuid(), isActive: true), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Updates_names_reorder_and_calls_UpdateAsync()
    {
        var category = ResourceCategory.Create("قديم", "Old", "old-slug", null, 1);
        var service = Substitute.For<IResourceCategoryRepository>();
        service.FindAsync(category.Id, Arg.Any<CancellationToken>()).Returns(category);
        var sut = new UpdateResourceCategoryCommandHandler(service);

        var cmd = new UpdateResourceCategoryCommand(category.Id, "جديد", "New", 10, true);
        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result!.NameAr.Should().Be("جديد");
        result.NameEn.Should().Be("New");
        result.OrderIndex.Should().Be(10);
        result.IsActive.Should().BeTrue();
        await service.Received(1).UpdateAsync(category, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deactivates_when_IsActive_is_false()
    {
        var category = ResourceCategory.Create("نشط", "Active", "active-cat", null, 0);
        var service = Substitute.For<IResourceCategoryRepository>();
        service.FindAsync(category.Id, Arg.Any<CancellationToken>()).Returns(category);
        var sut = new UpdateResourceCategoryCommandHandler(service);

        var cmd = new UpdateResourceCategoryCommand(category.Id, "نشط", "Active", 0, false);
        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result!.IsActive.Should().BeFalse();
        category.IsActive.Should().BeFalse();
    }

    private static UpdateResourceCategoryCommand BuildCommand(System.Guid id, bool isActive) =>
        new(id, "اسم عربي", "English Name", 0, isActive);
}
