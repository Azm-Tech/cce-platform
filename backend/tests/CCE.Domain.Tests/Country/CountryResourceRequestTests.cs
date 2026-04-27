using CCE.Domain.Common;
using CCE.Domain.Content;
using CCE.Domain.Country;
using CCE.Domain.Country.Events;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Country;

public class CountryResourceRequestTests
{
    private static FakeSystemClock NewClock() => new();

    private static CountryResourceRequest NewPending(FakeSystemClock clock) =>
        CountryResourceRequest.Submit(
            countryId: System.Guid.NewGuid(),
            requestedById: System.Guid.NewGuid(),
            titleAr: "عنوان", titleEn: "Title",
            descriptionAr: "وصف", descriptionEn: "Description",
            resourceType: ResourceType.Pdf,
            assetFileId: System.Guid.NewGuid(),
            clock: clock);

    [Fact]
    public void Submit_creates_pending_request()
    {
        var clock = NewClock();
        var r = NewPending(clock);

        r.Status.Should().Be(CountryResourceRequestStatus.Pending);
        r.SubmittedOn.Should().Be(clock.UtcNow);
        r.AdminNotesAr.Should().BeNull();
        r.ProcessedOn.Should().BeNull();
    }

    [Fact]
    public void Approve_transitions_to_Approved_and_raises_event()
    {
        var clock = NewClock();
        var r = NewPending(clock);
        var admin = System.Guid.NewGuid();
        clock.Advance(System.TimeSpan.FromHours(1));

        r.Approve(admin, "ملاحظة", "Note", clock);

        r.Status.Should().Be(CountryResourceRequestStatus.Approved);
        r.ProcessedById.Should().Be(admin);
        r.ProcessedOn.Should().Be(clock.UtcNow);
        r.AdminNotesAr.Should().Be("ملاحظة");
        r.DomainEvents.OfType<CountryResourceRequestApprovedEvent>().Should().HaveCount(1);
    }

    [Fact]
    public void Reject_requires_admin_notes_in_both_locales()
    {
        var clock = NewClock();
        var r = NewPending(clock);
        var act = () => r.Reject(System.Guid.NewGuid(), "", "Note", clock);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Reject_transitions_to_Rejected_and_raises_event()
    {
        var clock = NewClock();
        var r = NewPending(clock);
        var admin = System.Guid.NewGuid();

        r.Reject(admin, "سبب", "Reason", clock);

        r.Status.Should().Be(CountryResourceRequestStatus.Rejected);
        r.AdminNotesAr.Should().Be("سبب");
        r.DomainEvents.OfType<CountryResourceRequestRejectedEvent>().Should().HaveCount(1);
    }

    [Fact]
    public void Approving_already_processed_throws()
    {
        var clock = NewClock();
        var r = NewPending(clock);
        r.Approve(System.Guid.NewGuid(), null, null, clock);
        var act = () => r.Approve(System.Guid.NewGuid(), null, null, clock);
        act.Should().Throw<DomainException>().WithMessage("*Pending*");
    }

    [Fact]
    public void Rejecting_after_approval_throws()
    {
        var clock = NewClock();
        var r = NewPending(clock);
        r.Approve(System.Guid.NewGuid(), null, null, clock);
        var act = () => r.Reject(System.Guid.NewGuid(), "ا", "a", clock);
        act.Should().Throw<DomainException>();
    }
}
