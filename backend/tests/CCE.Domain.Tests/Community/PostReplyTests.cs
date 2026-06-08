using CCE.Domain.Common;
using CCE.Domain.Community;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Community;

public class PostReplyTests
{
    private static FakeSystemClock NewClock() => new();

    private static PostReply NewReply(FakeSystemClock clock, bool expert = false) =>
        PostReply.CreateRoot(
            System.Guid.NewGuid(), System.Guid.NewGuid(),
            "إجابة", "ar", expert, clock);

    [Fact]
    public void Create_top_level_reply()
    {
        var r = NewReply(NewClock());
        r.ParentReplyId.Should().BeNull();
        r.IsByExpert.Should().BeFalse();
        r.Depth.Should().Be(0);
    }

    [Fact]
    public void Create_threaded_reply_has_parent()
    {
        var clock = NewClock();
        var parent = NewReply(clock);
        var child = PostReply.CreateChild(parent, System.Guid.NewGuid(), "x", "ar", false, clock);
        child.ParentReplyId.Should().Be(parent.Id);
        parent.ChildCount.Should().Be(1);
    }

    [Fact]
    public void Expert_flag_persisted_at_creation()
    {
        var r = NewReply(NewClock(), expert: true);
        r.IsByExpert.Should().BeTrue();
    }

    [Fact]
    public void Content_over_8000_throws()
    {
        var clock = NewClock();
        var huge = new string('x', PostReply.MaxContentLength + 1);
        var act = () => PostReply.CreateRoot(System.Guid.NewGuid(), System.Guid.NewGuid(),
            huge, "ar", false, clock);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Invalid_locale_throws()
    {
        var clock = NewClock();
        var act = () => PostReply.CreateRoot(System.Guid.NewGuid(), System.Guid.NewGuid(),
            "x", "fr", false, clock);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void EditContent_replaces_text()
    {
        var clock = NewClock();
        var r = NewReply(clock);
        var editor = System.Guid.NewGuid();
        clock.Advance(System.TimeSpan.FromMinutes(1));
        r.EditContent("جديد", editor, clock);
        r.Content.Should().Be("جديد");
        r.LastModifiedOn.Should().Be(clock.UtcNow);
        r.LastModifiedById.Should().Be(editor);
    }

    [Fact]
    public void SoftDelete_marks_deleted()
    {
        var r = NewReply(NewClock());
        r.SoftDelete(System.Guid.NewGuid(), NewClock());
        r.IsDeleted.Should().BeTrue();
    }
}
