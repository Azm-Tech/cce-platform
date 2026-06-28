using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Commands.CreateResource;
using CCE.Application.Localization;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Content;
using DomainCountry = CCE.Domain.Country;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Content.Commands;

public class CreateResourceCommandHandlerTests
{
    private static readonly FakeSystemClock Clock = new();

    [Fact]
    public async Task Returns_asset_not_found_when_asset_missing()
    {
        var (sut, _, _) = BuildSut(Array.Empty<AssetFile>());

        var result = await sut.Handle(BuildCmd(System.Guid.NewGuid()), CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Returns_asset_not_clean_when_asset_not_scanned()
    {
        var pendingAsset = AssetFile.Register("k", "x.pdf", 1, "application/pdf", System.Guid.NewGuid(), Clock);

        var (sut, _, _) = BuildSut([pendingAsset]);

        var result = await sut.Handle(BuildCmd(pendingAsset.Id), CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Returns_not_authenticated_when_actor_unknown()
    {
        var clean = AssetFile.Register("k", "x.pdf", 1, "application/pdf", System.Guid.NewGuid(), Clock);
        clean.MarkClean(Clock);

        var category = ResourceCategory.Create("cat-ar", "cat-en", "cat-1", null, 1);
        var country = DomainCountry.Country.Register("SAU", "SA", "السعودية", "Saudi Arabia", "MENA", "MENA", "https://flag");

        var (sut, _, _) = BuildSut([clean], noUser: true, categoryId: category.Id, countryId: country.Id);

        var result = await sut.Handle(BuildCmd(clean.Id, category.Id, country.Id), CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Returns_dto_and_saves_when_inputs_valid()
    {
        var clean = AssetFile.Register("k", "x.pdf", 1, "application/pdf", System.Guid.NewGuid(), Clock);
        clean.MarkClean(Clock);

        var category = ResourceCategory.Create("cat-ar", "cat-en", "cat-1", null, 1);
        var country = DomainCountry.Country.Register("SAU", "SA", "السعودية", "Saudi Arabia", "MENA", "MENA", "https://flag");

        var (sut, repo, db) = BuildSut([clean], categoryId: category.Id, countryId: country.Id);

        var result = await sut.Handle(BuildCmd(clean.Id, category.Id, country.Id), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBe(System.Guid.Empty);
        await repo.Received(1).AddAsync(Arg.Any<Resource>(), Arg.Any<CancellationToken>());
        await db.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static CreateResourceCommand BuildCmd(System.Guid assetFileId, System.Guid? categoryId = null, System.Guid? countryId = null)
    {
        var catId = categoryId ?? System.Guid.NewGuid();
        var cId = countryId ?? System.Guid.NewGuid();
        return new(
            "عنوان", "Title",
            "وصف", "Description",
            ResourceType.Paper,
            catId,
            null,
            assetFileId,
            new[] { cId });
    }

    private static (CreateResourceCommandHandler sut,
        IRepository<Resource, System.Guid> repo,
        ICceDbContext db) BuildSut(IEnumerable<AssetFile> assets, bool noUser = false, System.Guid? categoryId = null, System.Guid? countryId = null)
    {
        var repo = Substitute.For<IRepository<Resource, System.Guid>>();
        var db = Substitute.For<ICceDbContext>();
        db.AssetFiles.Returns(assets.AsQueryable());

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
        if (countryId.HasValue)
        {
            var cty = (DomainCountry.Country)System.Activator.CreateInstance(
                typeof(DomainCountry.Country),
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                null,
                new object?[] { countryId.Value, "SAU", "SA", "السعودية", "Saudi Arabia", "MENA", "MENA", "https://flag" },
                null)!;
            db.Countries.Returns(new[] { cty }.AsQueryable());
        }

        var user = Substitute.For<ICurrentUserAccessor>();
        if (noUser)
            user.GetUserId().Returns((System.Guid?)null);
        else
            user.GetUserId().Returns(System.Guid.NewGuid());

        var localization = Substitute.For<ILocalizationService>();
        localization.GetString(Arg.Any<string>(), Arg.Any<string?>()).Returns(call => call.ArgAt<string>(0));

        var sut = new CreateResourceCommandHandler(repo, db, user, Clock, new MessageFactory(localization, Microsoft.Extensions.Logging.Abstractions.NullLogger<MessageFactory>.Instance));
        return (sut, repo, db);
    }
}
