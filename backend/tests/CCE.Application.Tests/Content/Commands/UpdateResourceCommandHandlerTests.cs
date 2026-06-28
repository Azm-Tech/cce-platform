using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Commands.UpdateResource;
using CCE.Application.Localization;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Content.Commands;

public class UpdateResourceCommandHandlerTests
{
    private static readonly FakeSystemClock Clock = new();

    [Fact]
    public async Task Returns_not_found_when_resource_missing()
    {
        var (sut, _) = BuildSut(null);

        var result = await sut.Handle(BuildCommand(System.Guid.NewGuid()), CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Updates_content_and_saves()
    {
        var resource = Resource.Draft(
            "old-ar", "old-en", "old-desc-ar", "old-desc-en",
            ResourceType.Paper, System.Guid.NewGuid(), null,
            System.Guid.NewGuid(), System.Guid.NewGuid(),
            System.Array.Empty<System.Guid>(), Clock);

        var category = ResourceCategory.Create("cat-ar", "cat-en", "cat-1", null, 1);
        var (sut, db) = BuildSut(resource, categoryId: category.Id);

        var cmd = new UpdateResourceCommand(
            resource.Id,
            "new-ar", "new-en", "new-desc-ar", "new-desc-en",
            ResourceType.Article, category.Id,
            System.Array.Empty<System.Guid>());

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.TitleEn.Should().Be("new-en");
        result.Data.ResourceType.Should().Be(ResourceType.Article);
        await db.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static UpdateResourceCommand BuildCommand(System.Guid id) =>
        new(id, "ar", "en", "desc-ar", "desc-en", ResourceType.Paper, System.Guid.NewGuid(),
            System.Array.Empty<System.Guid>());

    private static (UpdateResourceCommandHandler sut, ICceDbContext db) BuildSut(Resource? resourceToReturn, System.Guid? categoryId = null)
    {
        var repo = Substitute.For<IRepository<Resource, System.Guid>>();
        repo.GetByIdAsync(
                Arg.Any<System.Guid>(),
                Arg.Any<System.Func<System.Linq.IQueryable<Resource>, System.Linq.IQueryable<Resource>>>(),
                Arg.Any<CancellationToken>())
            .Returns(resourceToReturn);

        var db = Substitute.For<ICceDbContext>();

        if (categoryId.HasValue)
        {
            var cat = (ResourceCategory)System.Activator.CreateInstance(
                typeof(ResourceCategory),
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                null,
                new object?[] { categoryId.Value, "cat-ar", "cat-en", "cat-1", null, 1 },
                null)!;
            db.ResourceCategories.Returns(new[] { cat }.AsQueryable());
        }

        var localization = Substitute.For<ILocalizationService>();
        localization.GetString(Arg.Any<string>(), Arg.Any<string?>()).Returns(call => call.ArgAt<string>(0));
        return (new UpdateResourceCommandHandler(repo, db, new MessageFactory(localization, Microsoft.Extensions.Logging.Abstractions.NullLogger<MessageFactory>.Instance)), db);
    }
}
