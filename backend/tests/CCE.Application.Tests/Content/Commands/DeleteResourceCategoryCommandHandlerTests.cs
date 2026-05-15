using CCE.Application.Content;
using CCE.Application.Content.Commands.DeleteResourceCategory;
using CCE.Domain.Content;

namespace CCE.Application.Tests.Content.Commands;

public class DeleteResourceCategoryCommandHandlerTests
{
    [Fact]
    public async Task Throws_KeyNotFoundException_when_category_not_found()
    {
        var service = Substitute.For<IResourceCategoryRepository>();
        service.FindAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>()).Returns((ResourceCategory?)null);
        var sut = new DeleteResourceCategoryCommandHandler(service);

        var act = async () => await sut.Handle(new DeleteResourceCategoryCommand(System.Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<System.Collections.Generic.KeyNotFoundException>();
    }

    [Fact]
    public async Task Deactivates_category_and_calls_UpdateAsync()
    {
        var category = ResourceCategory.Create("نشط", "Active", "active-del", null, 0);
        var service = Substitute.For<IResourceCategoryRepository>();
        service.FindAsync(category.Id, Arg.Any<CancellationToken>()).Returns(category);
        var sut = new DeleteResourceCategoryCommandHandler(service);

        await sut.Handle(new DeleteResourceCategoryCommand(category.Id), CancellationToken.None);

        category.IsActive.Should().BeFalse();
        await service.Received(1).UpdateAsync(category, Arg.Any<CancellationToken>());
    }
}
