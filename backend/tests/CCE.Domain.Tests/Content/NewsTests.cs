using CCE.Domain.Common;
using CCE.Domain.Content;
using CCE.Domain.Content.Events;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Content;

public class NewsTests
{
    private static FakeSystemClock NewClock() => new();

    private static News NewDraft(FakeSystemClock clock) =>
        News.Draft(
            titleAr: "خبر",
            titleEn: "News",
            contentAr: "محتوى",
            contentEn: "Content",
            slug: "first-post",
            authorId: System.Guid.NewGuid(),
            featuredImageUrl: null,
            clock: clock);

    [Fact]
    public void Draft_creates_unpublished_news()
    {
        var clock = NewClock();
        var n = NewDraft(clock);

        n.PublishedOn.Should().BeNull();
        n.IsFeatured.Should().BeFalse();
    }

    [Fact]
    public void Slug_must_be_kebab_case()
    {
        var clock = NewClock();
        var act = () => News.Draft("ا", "x", "ا", "x", "Bad Slug", System.Guid.NewGuid(), null, clock);
        act.Should().Throw<DomainException>().WithMessage("*slug*");
    }

    [Fact]
    public void FeaturedImageUrl_must_be_https()
    {
        var clock = NewClock();
        var act = () => News.Draft("ا", "x", "ا", "x", "x", System.Guid.NewGuid(), "http://insecure", clock);
        act.Should().Throw<DomainException>().WithMessage("*https*");
    }

    [Fact]
    public void Publish_sets_PublishedOn_and_raises_event()
    {
        var clock = NewClock();
        var n = NewDraft(clock);
        clock.Advance(System.TimeSpan.FromMinutes(5));

        n.Publish(clock);

        n.PublishedOn.Should().Be(clock.UtcNow);
        n.DomainEvents.OfType<NewsPublishedEvent>().Should().HaveCount(1);
    }

    [Fact]
    public void MarkFeatured_sets_IsFeatured_true()
    {
        var clock = NewClock();
        var n = NewDraft(clock);
        n.MarkFeatured();
        n.IsFeatured.Should().BeTrue();
    }

    [Fact]
    public void UnmarkFeatured_clears_flag()
    {
        var clock = NewClock();
        var n = NewDraft(clock);
        n.MarkFeatured();
        n.UnmarkFeatured();
        n.IsFeatured.Should().BeFalse();
    }

    [Fact]
    public void SoftDelete_marks_deleted()
    {
        var clock = NewClock();
        var n = NewDraft(clock);
        n.SoftDelete(System.Guid.NewGuid(), clock);
        n.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void UpdateContent_mutates_editable_fields_when_inputs_valid()
    {
        var clock = NewClock();
        var n = NewDraft(clock);

        n.UpdateContent(
            titleAr: "خبر جديد",
            titleEn: "New News",
            contentAr: "محتوى جديد",
            contentEn: "New Content",
            slug: "new-slug",
            featuredImageUrl: "https://example.com/image.jpg");

        n.TitleAr.Should().Be("خبر جديد");
        n.TitleEn.Should().Be("New News");
        n.ContentAr.Should().Be("محتوى جديد");
        n.ContentEn.Should().Be("New Content");
        n.Slug.Should().Be("new-slug");
        n.FeaturedImageUrl.Should().Be("https://example.com/image.jpg");
    }

    [Fact]
    public void UpdateContent_throws_DomainException_when_slug_not_kebab_case()
    {
        var clock = NewClock();
        var n = NewDraft(clock);

        var act = () => n.UpdateContent("خبر", "News", "محتوى", "Content", "Bad Slug!", null);

        act.Should().Throw<DomainException>().WithMessage("*slug*");
    }
}
