using CCE.Application.Common.Interfaces;
using CCE.Application.Kapsarc.Queries.GetLatestKapsarcSnapshot;
using CCE.Domain.Common;
using CCE.Domain.Country;

namespace CCE.Application.Tests.Kapsarc;

public class GetLatestKapsarcSnapshotQueryHandlerTests
{
    private static CountryKapsarcSnapshot MakeSnapshot(System.Guid countryId, System.DateTimeOffset takenOn)
    {
        var clock = Substitute.For<ISystemClock>();
        clock.UtcNow.Returns(takenOn);
        return CountryKapsarcSnapshot.Capture(countryId, "A", 80m, 75m, clock, "v1");
    }

    [Fact]
    public async Task Returns_null_when_no_snapshots_exist()
    {
        var db = Substitute.For<ICceDbContext>();
        db.CountryKapsarcSnapshots.Returns(Array.Empty<CountryKapsarcSnapshot>().AsQueryable());

        var sut = new GetLatestKapsarcSnapshotQueryHandler(db);
        var result = await sut.Handle(new GetLatestKapsarcSnapshotQuery(System.Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Returns_latest_snapshot_by_SnapshotTakenOn()
    {
        var countryId = System.Guid.NewGuid();
        var older = MakeSnapshot(countryId, System.DateTimeOffset.UtcNow.AddDays(-10));
        var newer = MakeSnapshot(countryId, System.DateTimeOffset.UtcNow.AddDays(-1));

        var db = Substitute.For<ICceDbContext>();
        db.CountryKapsarcSnapshots.Returns(new[] { older, newer }.AsQueryable());

        var sut = new GetLatestKapsarcSnapshotQueryHandler(db);
        var result = await sut.Handle(new GetLatestKapsarcSnapshotQuery(countryId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(newer.Id);
    }

    [Fact]
    public async Task Filters_by_country_id()
    {
        var targetCountry = System.Guid.NewGuid();
        var otherCountry = System.Guid.NewGuid();
        var snap = MakeSnapshot(targetCountry, System.DateTimeOffset.UtcNow);
        var other = MakeSnapshot(otherCountry, System.DateTimeOffset.UtcNow);

        var db = Substitute.For<ICceDbContext>();
        db.CountryKapsarcSnapshots.Returns(new[] { snap, other }.AsQueryable());

        var sut = new GetLatestKapsarcSnapshotQueryHandler(db);
        var result = await sut.Handle(new GetLatestKapsarcSnapshotQuery(targetCountry), CancellationToken.None);

        result.Should().NotBeNull();
        result!.CountryId.Should().Be(targetCountry);
    }

    [Fact]
    public async Task Maps_snapshot_fields_correctly()
    {
        var countryId = System.Guid.NewGuid();
        var snap = MakeSnapshot(countryId, System.DateTimeOffset.UtcNow);

        var db = Substitute.For<ICceDbContext>();
        db.CountryKapsarcSnapshots.Returns(new[] { snap }.AsQueryable());

        var sut = new GetLatestKapsarcSnapshotQueryHandler(db);
        var result = await sut.Handle(new GetLatestKapsarcSnapshotQuery(countryId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Classification.Should().Be("A");
        result.PerformanceScore.Should().Be(80m);
        result.TotalIndex.Should().Be(75m);
        result.SourceVersion.Should().Be("v1");
    }
}
