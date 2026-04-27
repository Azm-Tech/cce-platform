using CCE.Domain.Common;
using CCE.Domain.Identity;
using CCE.Domain.Identity.Events;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Identity;

public class ExpertRegistrationRequestTests
{
    private static FakeSystemClock NewClock() => new();

    private static ExpertRegistrationRequest NewPending(FakeSystemClock clock) =>
        ExpertRegistrationRequest.Submit(
            requesterId: System.Guid.NewGuid(),
            bioAr: "خبير",
            bioEn: "Expert",
            tags: new[] { "Solar", "Storage" },
            clock: clock);

    [Fact]
    public void Submit_factory_creates_pending_request()
    {
        var clock = NewClock();
        var req = NewPending(clock);

        req.Status.Should().Be(ExpertRegistrationStatus.Pending);
        req.RequestedBioAr.Should().Be("خبير");
        req.RequestedBioEn.Should().Be("Expert");
        req.RequestedTags.Should().Equal("Solar", "Storage");
        req.ProcessedOn.Should().BeNull();
        req.ProcessedById.Should().BeNull();
        req.RejectionReasonAr.Should().BeNull();
        req.RejectionReasonEn.Should().BeNull();
        req.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Submit_with_empty_bios_throws()
    {
        var clock = NewClock();
        var act1 = () => ExpertRegistrationRequest.Submit(System.Guid.NewGuid(), "", "Expert", new[] { "x" }, clock);
        var act2 = () => ExpertRegistrationRequest.Submit(System.Guid.NewGuid(), "خبير", "", new[] { "x" }, clock);
        act1.Should().Throw<DomainException>();
        act2.Should().Throw<DomainException>();
    }

    [Fact]
    public void Submit_with_empty_requesterId_throws()
    {
        var clock = NewClock();
        var act = () => ExpertRegistrationRequest.Submit(System.Guid.Empty, "خبير", "Expert", new[] { "x" }, clock);
        act.Should().Throw<DomainException>().WithMessage("*RequesterId*");
    }

    [Fact]
    public void Approve_transitions_to_Approved_and_records_processor()
    {
        var clock = NewClock();
        var req = NewPending(clock);
        clock.Advance(System.TimeSpan.FromHours(1));
        var admin = System.Guid.NewGuid();

        req.Approve(admin, clock);

        req.Status.Should().Be(ExpertRegistrationStatus.Approved);
        req.ProcessedById.Should().Be(admin);
        req.ProcessedOn.Should().Be(clock.UtcNow);
    }

    [Fact]
    public void Approve_raises_ExpertRegistrationApprovedEvent()
    {
        var clock = NewClock();
        var req = NewPending(clock);
        var admin = System.Guid.NewGuid();
        req.Approve(admin, clock);

        req.DomainEvents.Should().HaveCount(1);
        var evt = req.DomainEvents.OfType<ExpertRegistrationApprovedEvent>().Single();
        evt.RequestId.Should().Be(req.Id);
        evt.ApprovedById.Should().Be(admin);
        evt.RequestedTags.Should().Equal("Solar", "Storage");
    }

    [Fact]
    public void Reject_transitions_to_Rejected_and_records_reasons()
    {
        var clock = NewClock();
        var req = NewPending(clock);
        var admin = System.Guid.NewGuid();

        req.Reject(admin, "سبب", "Reason", clock);

        req.Status.Should().Be(ExpertRegistrationStatus.Rejected);
        req.ProcessedById.Should().Be(admin);
        req.RejectionReasonAr.Should().Be("سبب");
        req.RejectionReasonEn.Should().Be("Reason");
    }

    [Fact]
    public void Reject_raises_ExpertRegistrationRejectedEvent()
    {
        var clock = NewClock();
        var req = NewPending(clock);
        var admin = System.Guid.NewGuid();
        req.Reject(admin, "سبب", "Reason", clock);

        req.DomainEvents.OfType<ExpertRegistrationRejectedEvent>().Should().HaveCount(1);
    }

    [Fact]
    public void Approving_already_processed_request_throws()
    {
        var clock = NewClock();
        var req = NewPending(clock);
        req.Approve(System.Guid.NewGuid(), clock);

        var act = () => req.Approve(System.Guid.NewGuid(), clock);
        act.Should().Throw<DomainException>().WithMessage("*Pending*");
    }

    [Fact]
    public void Rejecting_already_processed_request_throws()
    {
        var clock = NewClock();
        var req = NewPending(clock);
        req.Reject(System.Guid.NewGuid(), "ا", "a", clock);

        var act = () => req.Reject(System.Guid.NewGuid(), "ب", "b", clock);
        act.Should().Throw<DomainException>().WithMessage("*Pending*");
    }

    [Fact]
    public void Approving_after_rejection_throws()
    {
        var clock = NewClock();
        var req = NewPending(clock);
        req.Reject(System.Guid.NewGuid(), "ا", "a", clock);

        var act = () => req.Approve(System.Guid.NewGuid(), clock);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Reject_with_empty_reason_throws()
    {
        var clock = NewClock();
        var req = NewPending(clock);

        var act = () => req.Reject(System.Guid.NewGuid(), "", "", clock);
        act.Should().Throw<DomainException>();
    }
}
