using CCE.Application.Common.Interfaces;
using CCE.Application.Identity.Public;
using CCE.Application.Identity.Public.Commands.SubmitExpertRequest;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Content;
using CCE.Domain.Identity;
using CCE.TestInfrastructure.Time;
using static CCE.Application.Tests.Identity.IdentityTestHelpers;

namespace CCE.Application.Tests.Identity.Public.Commands;

public class SubmitExpertRequestCommandHandlerTests
{
    [Fact]
    public async Task Persists_request_and_returns_dto()
    {
        var clock = new FakeSystemClock();
        var cvAsset = AssetFile.Register("https://cdn.example.com/cv.pdf", "cv.pdf", 1024, "application/pdf", System.Guid.NewGuid(), clock);
        cvAsset.MarkClean(clock);

        var db = Substitute.For<ICceDbContext>();
        db.AssetFiles.Returns(new[] { cvAsset }.AsQueryable());

        var service = Substitute.For<IExpertRequestSubmissionRepository>();
        var sut = new SubmitExpertRequestCommandHandler(db, service, clock, BuildMsg());

        var requesterId = System.Guid.NewGuid();
        var cmd = new SubmitExpertRequestCommand(
            requesterId,
            "سيرة ذاتية",
            "English bio",
            new[] { "Hydrogen", "Solar" },
            cvAsset.Id);

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.RequestedById.Should().Be(requesterId);
        result.Data.RequestedBioAr.Should().Be("سيرة ذاتية");
        result.Data.RequestedBioEn.Should().Be("English bio");
        result.Data.RequestedTags.Should().BeEquivalentTo(new[] { "Hydrogen", "Solar" });
        result.Data.Status.Should().Be(ExpertRegistrationStatus.Pending);
        result.Data.ProcessedOn.Should().BeNull();
        await service.Received(1).AddAsync(Arg.Any<ExpertRegistrationRequest>(), Arg.Any<CancellationToken>());
        await db.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Domain_throws_when_tags_are_empty()
    {
        var clock = new FakeSystemClock();
        var cvAsset = AssetFile.Register("https://cdn.example.com/cv.pdf", "cv.pdf", 1024, "application/pdf", System.Guid.NewGuid(), clock);
        cvAsset.MarkClean(clock);

        var db = Substitute.For<ICceDbContext>();
        db.AssetFiles.Returns(new[] { cvAsset }.AsQueryable());

        var service = Substitute.For<IExpertRequestSubmissionRepository>();
        var sut = new SubmitExpertRequestCommandHandler(db, service, clock, BuildMsg());

        var cmd = new SubmitExpertRequestCommand(
            System.Guid.NewGuid(),
            "سيرة ذاتية",
            "English bio",
            System.Array.Empty<string>(),
            cvAsset.Id);

        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
        await service.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await db.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Returns_not_found_when_cv_asset_missing()
    {
        var clock = new FakeSystemClock();
        var db = Substitute.For<ICceDbContext>();
        db.AssetFiles.Returns(System.Array.Empty<AssetFile>().AsQueryable());

        var service = Substitute.For<IExpertRequestSubmissionRepository>();
        var sut = new SubmitExpertRequestCommandHandler(db, service, clock, BuildMsg());

        var cmd = new SubmitExpertRequestCommand(
            System.Guid.NewGuid(),
            "سيرة ذاتية",
            "English bio",
            new[] { "Hydrogen" },
            System.Guid.NewGuid());

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Success.Should().BeFalse();
        await service.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Returns_error_when_cv_asset_not_clean()
    {
        var clock = new FakeSystemClock();
        var cvAsset = AssetFile.Register("https://cdn.example.com/cv.pdf", "cv.pdf", 1024, "application/pdf", System.Guid.NewGuid(), clock);
        cvAsset.MarkInfected(clock);

        var db = Substitute.For<ICceDbContext>();
        db.AssetFiles.Returns(new[] { cvAsset }.AsQueryable());

        var service = Substitute.For<IExpertRequestSubmissionRepository>();
        var sut = new SubmitExpertRequestCommandHandler(db, service, clock, BuildMsg());

        var cmd = new SubmitExpertRequestCommand(
            System.Guid.NewGuid(),
            "سيرة ذاتية",
            "English bio",
            new[] { "Hydrogen" },
            cvAsset.Id);

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Success.Should().BeFalse();
        await service.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }
}
