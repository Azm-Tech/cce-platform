using CCE.Domain.Common;
using CCE.Domain.Content;
using CCE.Domain.Content.Events;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Content;

public class ResourceTests
{
    private static FakeSystemClock NewClock() => new();

    private static Resource NewDraft(FakeSystemClock clock, System.Guid? countryId = null) =>
        Resource.Draft(
            titleAr: "مورد",
            titleEn: "Resource",
            descriptionAr: "وصف",
            descriptionEn: "Description",
            resourceType: ResourceType.Pdf,
            categoryId: System.Guid.NewGuid(),
            countryId: countryId,
            uploadedById: System.Guid.NewGuid(),
            assetFileId: System.Guid.NewGuid(),
            clock: clock);

    [Fact]
    public void Draft_factory_creates_unpublished_resource()
    {
        var clock = NewClock();
        var r = NewDraft(clock);

        r.PublishedOn.Should().BeNull();
        r.ViewCount.Should().Be(0);
        r.IsDeleted.Should().BeFalse();
        r.RowVersion.Should().NotBeNull();
    }

    [Fact]
    public void Draft_with_null_country_marks_center_managed()
    {
        var clock = NewClock();
        var r = NewDraft(clock, countryId: null);
        r.IsCenterManaged.Should().BeTrue();
        r.CountryId.Should().BeNull();
    }

    [Fact]
    public void Draft_with_country_marks_country_managed()
    {
        var clock = NewClock();
        var country = System.Guid.NewGuid();
        var r = NewDraft(clock, countryId: country);
        r.IsCenterManaged.Should().BeFalse();
        r.CountryId.Should().Be(country);
    }

    [Fact]
    public void Draft_with_empty_titleAr_throws()
    {
        var clock = NewClock();
        var act = () => Resource.Draft("", "x", "x", "x", ResourceType.Pdf,
            System.Guid.NewGuid(), null, System.Guid.NewGuid(), System.Guid.NewGuid(), clock);
        act.Should().Throw<DomainException>().WithMessage("*TitleAr*");
    }

    [Fact]
    public void Publish_sets_PublishedOn_and_raises_event()
    {
        var clock = NewClock();
        var r = NewDraft(clock);
        clock.Advance(System.TimeSpan.FromMinutes(10));

        r.Publish(clock);

        r.PublishedOn.Should().Be(clock.UtcNow);
        r.IsPublished.Should().BeTrue();
        r.DomainEvents.OfType<ResourcePublishedEvent>().Should().HaveCount(1);
    }

    [Fact]
    public void Publishing_already_published_resource_is_noop()
    {
        var clock = NewClock();
        var r = NewDraft(clock);
        r.Publish(clock);
        var firstPublishedOn = r.PublishedOn;
        r.ClearDomainEvents();
        clock.Advance(System.TimeSpan.FromHours(1));

        r.Publish(clock);

        r.PublishedOn.Should().Be(firstPublishedOn);
        r.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void IncrementViewCount_increases_by_one()
    {
        var clock = NewClock();
        var r = NewDraft(clock);
        r.IncrementViewCount();
        r.IncrementViewCount();
        r.ViewCount.Should().Be(2);
    }

    [Fact]
    public void SoftDelete_marks_deleted()
    {
        var clock = NewClock();
        var r = NewDraft(clock);
        var deleter = System.Guid.NewGuid();
        r.SoftDelete(deleter, clock);

        r.IsDeleted.Should().BeTrue();
        r.DeletedById.Should().Be(deleter);
        r.DeletedOn.Should().Be(clock.UtcNow);
    }

    [Fact]
    public void UpdateContent_mutates_editable_fields_when_inputs_valid()
    {
        var clock = NewClock();
        var r = NewDraft(clock);
        var newCategoryId = System.Guid.NewGuid();

        r.UpdateContent("new-ar", "new-en", "new-desc-ar", "new-desc-en", ResourceType.Video, newCategoryId);

        r.TitleAr.Should().Be("new-ar");
        r.TitleEn.Should().Be("new-en");
        r.DescriptionAr.Should().Be("new-desc-ar");
        r.DescriptionEn.Should().Be("new-desc-en");
        r.ResourceType.Should().Be(ResourceType.Video);
        r.CategoryId.Should().Be(newCategoryId);
    }

    [Fact]
    public void UpdateContent_throws_DomainException_when_titleAr_empty()
    {
        var clock = NewClock();
        var r = NewDraft(clock);

        var act = () => r.UpdateContent("", "en", "desc-ar", "desc-en", ResourceType.Pdf, System.Guid.NewGuid());

        act.Should().Throw<DomainException>().WithMessage("*TitleAr*");
    }
}
