using CCE.Application.Content;
using CCE.Application.Content.Commands.CreateResourceCategory;

namespace CCE.Application.Tests.Content.Commands;

public class CreateResourceCategoryCommandHandlerTests
{
    [Fact]
    public async Task Creates_category_saves_and_returns_dto()
    {
        var service = Substitute.For<IResourceCategoryRepository>();
        var sut = new CreateResourceCategoryCommandHandler(service);

        var cmd = new CreateResourceCategoryCommand("طاقة", "Energy", "energy", null, 0);
        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.NameAr.Should().Be("طاقة");
        result.NameEn.Should().Be("Energy");
        result.Slug.Should().Be("energy");
        result.IsActive.Should().BeTrue();
        await service.Received(1).SaveAsync(Arg.Any<CCE.Domain.Content.ResourceCategory>(), Arg.Any<CancellationToken>());
    }
}
