using CCE.Domain.Common;
using CCE.Domain.Identity;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Identity;

public class ExpertProfileTests
{
    private static FakeSystemClock NewClock() => new();

    private static ExpertRegistrationRequest NewApproved(FakeSystemClock clock, out System.Guid approverId)
    {
        var req = ExpertRegistrationRequest.Submit(
            requesterId: System.Guid.NewGuid(),
            bioAr: "خبير الطاقة المتجددة",
            bioEn: "Renewable energy expert",
            tags: new[] { "Solar", "Wind" },
            clock: clock);
        approverId = System.Guid.NewGuid();
        req.Approve(approverId, clock);
        return req;
    }

    [Fact]
    public void CreateFromApprovedRequest_copies_request_fields()
    {
        var clock = NewClock();
        var req = NewApproved(clock, out var approverId);
        clock.Advance(System.TimeSpan.FromMinutes(5));

        var profile = ExpertProfile.CreateFromApprovedRequest(req, "د.", "Dr.", clock);

        profile.UserId.Should().Be(req.RequestedById);
        profile.BioAr.Should().Be("خبير الطاقة المتجددة");
        profile.BioEn.Should().Be("Renewable energy expert");
        profile.ExpertiseTags.Should().Equal("Solar", "Wind");
        profile.AcademicTitleAr.Should().Be("د.");
        profile.AcademicTitleEn.Should().Be("Dr.");
        profile.ApprovedOn.Should().Be(req.ProcessedOn!.Value);
        profile.ApprovedById.Should().Be(approverId);
        profile.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void CreateFromApprovedRequest_throws_when_request_is_pending()
    {
        var clock = NewClock();
        var pending = ExpertRegistrationRequest.Submit(
            System.Guid.NewGuid(), "ا", "a", new[] { "x" }, clock);

        var act = () => ExpertProfile.CreateFromApprovedRequest(pending, "د.", "Dr.", clock);
        act.Should().Throw<DomainException>().WithMessage("*Approved*");
    }

    [Fact]
    public void CreateFromApprovedRequest_throws_when_request_is_rejected()
    {
        var clock = NewClock();
        var rejected = ExpertRegistrationRequest.Submit(
            System.Guid.NewGuid(), "ا", "a", new[] { "x" }, clock);
        rejected.Reject(System.Guid.NewGuid(), "ر", "r", clock);

        var act = () => ExpertProfile.CreateFromApprovedRequest(rejected, "د.", "Dr.", clock);
        act.Should().Throw<DomainException>().WithMessage("*Approved*");
    }

    [Fact]
    public void UpdateBio_replaces_bilingual_bios()
    {
        var clock = NewClock();
        var req = NewApproved(clock, out _);
        var profile = ExpertProfile.CreateFromApprovedRequest(req, "د.", "Dr.", clock);

        profile.UpdateBio("نص جديد", "New text");

        profile.BioAr.Should().Be("نص جديد");
        profile.BioEn.Should().Be("New text");
    }

    [Fact]
    public void UpdateBio_with_empty_throws()
    {
        var clock = NewClock();
        var req = NewApproved(clock, out _);
        var profile = ExpertProfile.CreateFromApprovedRequest(req, "د.", "Dr.", clock);

        var act = () => profile.UpdateBio("", "");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void UpdateExpertiseTags_dedupes_and_trims()
    {
        var clock = NewClock();
        var req = NewApproved(clock, out _);
        var profile = ExpertProfile.CreateFromApprovedRequest(req, "د.", "Dr.", clock);

        profile.UpdateExpertiseTags(new[] { " Solar ", "solar", "Storage" });

        profile.ExpertiseTags.Should().Contain("Solar").And.Contain("Storage").And.Contain("solar");
        profile.ExpertiseTags.Should().HaveCount(3);
    }

    [Fact]
    public void UpdateAcademicTitle_replaces_titles()
    {
        var clock = NewClock();
        var req = NewApproved(clock, out _);
        var profile = ExpertProfile.CreateFromApprovedRequest(req, "د.", "Dr.", clock);

        profile.UpdateAcademicTitle("أستاذ", "Prof.");

        profile.AcademicTitleAr.Should().Be("أستاذ");
        profile.AcademicTitleEn.Should().Be("Prof.");
    }
}
