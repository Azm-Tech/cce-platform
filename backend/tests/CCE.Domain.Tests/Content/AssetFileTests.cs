using CCE.Domain.Common;
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Content;

public class AssetFileTests
{
    private static FakeSystemClock NewClock() => new();

    private static AssetFile NewPending(FakeSystemClock clock) =>
        AssetFile.Register(
            url: "https://cdn.example/uploads/abc.pdf",
            originalFileName: "report.pdf",
            sizeBytes: 12345,
            mimeType: "application/pdf",
            uploadedById: System.Guid.NewGuid(),
            clock: clock);

    [Fact]
    public void Register_creates_pending_asset()
    {
        var clock = NewClock();
        var asset = NewPending(clock);

        asset.Id.Should().NotBe(System.Guid.Empty);
        asset.Url.Should().Be("https://cdn.example/uploads/abc.pdf");
        asset.OriginalFileName.Should().Be("report.pdf");
        asset.SizeBytes.Should().Be(12345);
        asset.MimeType.Should().Be("application/pdf");
        asset.UploadedOn.Should().Be(clock.UtcNow);
        asset.VirusScanStatus.Should().Be(VirusScanStatus.Pending);
        asset.ScannedOn.Should().BeNull();
    }

    [Fact]
    public void Register_with_zero_size_throws()
    {
        var clock = NewClock();
        var act = () => AssetFile.Register("https://x", "f", 0, "x/y", System.Guid.NewGuid(), clock);
        act.Should().Throw<DomainException>().WithMessage("*SizeBytes*");
    }

    [Fact]
    public void Register_with_empty_url_throws()
    {
        var clock = NewClock();
        var act = () => AssetFile.Register("", "f", 1, "x/y", System.Guid.NewGuid(), clock);
        act.Should().Throw<DomainException>().WithMessage("*Url*");
    }

    [Fact]
    public void MarkClean_transitions_from_Pending()
    {
        var clock = NewClock();
        var asset = NewPending(clock);
        clock.Advance(System.TimeSpan.FromMinutes(2));

        asset.MarkClean(clock);

        asset.VirusScanStatus.Should().Be(VirusScanStatus.Clean);
        asset.ScannedOn.Should().Be(clock.UtcNow);
    }

    [Fact]
    public void MarkInfected_transitions_from_Pending()
    {
        var clock = NewClock();
        var asset = NewPending(clock);
        asset.MarkInfected(clock);

        asset.VirusScanStatus.Should().Be(VirusScanStatus.Infected);
    }

    [Fact]
    public void MarkScanFailed_transitions_from_Pending()
    {
        var clock = NewClock();
        var asset = NewPending(clock);
        asset.MarkScanFailed(clock);

        asset.VirusScanStatus.Should().Be(VirusScanStatus.ScanFailed);
    }

    [Fact]
    public void Cannot_transition_a_clean_asset_to_infected()
    {
        var clock = NewClock();
        var asset = NewPending(clock);
        asset.MarkClean(clock);

        var act = () => asset.MarkInfected(clock);
        act.Should().Throw<DomainException>().WithMessage("*Pending*");
    }

    [Fact]
    public void Cannot_double_mark_clean()
    {
        var clock = NewClock();
        var asset = NewPending(clock);
        asset.MarkClean(clock);

        var act = () => asset.MarkClean(clock);
        act.Should().Throw<DomainException>();
    }
}
