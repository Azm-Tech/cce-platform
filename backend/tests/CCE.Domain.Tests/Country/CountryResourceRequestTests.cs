using CCE.Domain.Common;
using CCE.Domain.Content;
using CCE.Domain.Country;
using CCE.Domain.Country.Events;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Country;

public class CountryContentRequestTests
{
    private static FakeSystemClock NewClock() => new();

    private static CountryContentRequest NewPendingResource(FakeSystemClock clock) =>
        CountryContentRequest.SubmitResource(
            countryId: System.Guid.NewGuid(),
            requestedById: System.Guid.NewGuid(),
            titleAr: "عنوان", titleEn: "Title",
            descriptionAr: "وصف", descriptionEn: "Description",
            resourceType: ResourceType.Paper,
            assetFileId: System.Guid.NewGuid(),
            categoryId: System.Guid.NewGuid(),
            clock: clock);

    // ─── SubmitResource ───────────────────────────────────────────────────────

    [Fact]
    public void SubmitResource_creates_pending_resource_request()
    {
        var clock = NewClock();
        var r = NewPendingResource(clock);

        r.Type.Should().Be(ContentType.Resource);
        r.Status.Should().Be(CountryContentRequestStatus.Pending);
        r.SubmittedOn.Should().Be(clock.UtcNow);
        r.AdminNotesAr.Should().BeNull();
        r.ProcessedOn.Should().BeNull();
        r.ProposedResourceType.Should().Be(ResourceType.Paper);
        r.ProposedTopicId.Should().BeNull();
    }

    [Theory]
    [InlineData("", "Title", "وصف", "Desc")]
    [InlineData("عنوان", "", "وصف", "Desc")]
    [InlineData("عنوان", "Title", "", "Desc")]
    [InlineData("عنوان", "Title", "وصف", "")]
    public void SubmitResource_with_empty_required_field_throws(
        string titleAr, string titleEn, string descAr, string descEn)
    {
        var clock = NewClock();
        var act = () => CountryContentRequest.SubmitResource(
            System.Guid.NewGuid(), System.Guid.NewGuid(),
            titleAr, titleEn, descAr, descEn,
            ResourceType.Paper, System.Guid.NewGuid(), System.Guid.NewGuid(), clock);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void SubmitResource_with_empty_assetFileId_throws()
    {
        var clock = NewClock();
        var act = () => CountryContentRequest.SubmitResource(
            System.Guid.NewGuid(), System.Guid.NewGuid(),
            "ا", "x", "ا", "x",
            ResourceType.Paper, System.Guid.Empty, System.Guid.NewGuid(), clock);
        act.Should().Throw<DomainException>().WithMessage("*AssetFileId*");
    }

    // ─── SubmitNews ───────────────────────────────────────────────────────────

    [Fact]
    public void SubmitNews_creates_pending_news_request()
    {
        var clock = NewClock();
        var topicId = System.Guid.NewGuid();
        var r = CountryContentRequest.SubmitNews(
            System.Guid.NewGuid(), System.Guid.NewGuid(),
            "عنوان", "Title", "محتوى", "Content",
            topicId, null, clock);

        r.Type.Should().Be(ContentType.News);
        r.Status.Should().Be(CountryContentRequestStatus.Pending);
        r.ProposedTopicId.Should().Be(topicId);
        r.ProposedResourceType.Should().BeNull();
        r.ProposedStartsOn.Should().BeNull();
    }

    [Fact]
    public void SubmitNews_with_empty_topicId_throws()
    {
        var clock = NewClock();
        var act = () => CountryContentRequest.SubmitNews(
            System.Guid.NewGuid(), System.Guid.NewGuid(),
            "ا", "x", "ا", "x",
            System.Guid.Empty, null, clock);
        act.Should().Throw<DomainException>().WithMessage("*TopicId*");
    }

    // ─── SubmitEvent ──────────────────────────────────────────────────────────

    [Fact]
    public void SubmitEvent_creates_pending_event_request()
    {
        var clock = NewClock();
        var topicId = System.Guid.NewGuid();
        var start = clock.UtcNow.AddDays(1);
        var end = clock.UtcNow.AddDays(2);

        var r = CountryContentRequest.SubmitEvent(
            System.Guid.NewGuid(), System.Guid.NewGuid(),
            "عنوان", "Title", "وصف", "Description",
            topicId, start, end, "الرياض", "Riyadh", null, null, clock);

        r.Type.Should().Be(ContentType.Event);
        r.ProposedStartsOn.Should().Be(start);
        r.ProposedEndsOn.Should().Be(end);
        r.ProposedLocationAr.Should().Be("الرياض");
        r.ProposedResourceType.Should().BeNull();
    }

    [Fact]
    public void SubmitEvent_with_startsOn_after_endsOn_throws()
    {
        var clock = NewClock();
        var act = () => CountryContentRequest.SubmitEvent(
            System.Guid.NewGuid(), System.Guid.NewGuid(),
            "ا", "x", "ا", "x",
            System.Guid.NewGuid(),
            clock.UtcNow.AddDays(2), clock.UtcNow.AddDays(1),
            null, null, null, null, clock);
        act.Should().Throw<DomainException>().WithMessage("*StartsOn*");
    }

    // ─── Approve / Reject ─────────────────────────────────────────────────────

    [Fact]
    public void Approve_transitions_to_Approved_and_raises_event()
    {
        var clock = NewClock();
        var r = NewPendingResource(clock);
        var admin = System.Guid.NewGuid();
        clock.Advance(System.TimeSpan.FromHours(1));

        r.Approve(admin, "ملاحظة", "Note", clock);

        r.Status.Should().Be(CountryContentRequestStatus.Approved);
        r.ProcessedById.Should().Be(admin);
        r.ProcessedOn.Should().Be(clock.UtcNow);
        r.AdminNotesAr.Should().Be("ملاحظة");
        r.DomainEvents.OfType<CountryContentRequestApprovedEvent>().Should().HaveCount(1);
        r.DomainEvents.OfType<CountryContentRequestApprovedEvent>().Single().Type.Should().Be(ContentType.Resource);
    }

    [Fact]
    public void Reject_requires_admin_notes_in_both_locales()
    {
        var clock = NewClock();
        var r = NewPendingResource(clock);
        var act = () => r.Reject(System.Guid.NewGuid(), "", "Note", clock);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Reject_transitions_to_Rejected_and_raises_event()
    {
        var clock = NewClock();
        var r = NewPendingResource(clock);
        var admin = System.Guid.NewGuid();

        r.Reject(admin, "سبب", "Reason", clock);

        r.Status.Should().Be(CountryContentRequestStatus.Rejected);
        r.AdminNotesAr.Should().Be("سبب");
        r.DomainEvents.OfType<CountryContentRequestRejectedEvent>().Should().HaveCount(1);
        r.DomainEvents.OfType<CountryContentRequestRejectedEvent>().Single().Type.Should().Be(ContentType.Resource);
    }

    [Fact]
    public void Approving_already_processed_throws()
    {
        var clock = NewClock();
        var r = NewPendingResource(clock);
        r.Approve(System.Guid.NewGuid(), null, null, clock);
        var act = () => r.Approve(System.Guid.NewGuid(), null, null, clock);
        act.Should().Throw<DomainException>().WithMessage("*Pending*");
    }

    [Fact]
    public void Rejecting_after_approval_throws()
    {
        var clock = NewClock();
        var r = NewPendingResource(clock);
        r.Approve(System.Guid.NewGuid(), null, null, clock);
        var act = () => r.Reject(System.Guid.NewGuid(), "ا", "a", clock);
        act.Should().Throw<DomainException>();
    }
}
