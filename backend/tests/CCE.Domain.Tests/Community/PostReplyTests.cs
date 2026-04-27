using CCE.Domain.Common;
using CCE.Domain.Community;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Community;

public class PostReplyTests
{
    private static FakeSystemClock NewClock() => new();

    private static PostReply NewReply(FakeSystemClock clock, System.Guid? parent = null, bool expert = false) =>
        PostReply.Create(
            System.Guid.NewGuid(), System.Guid.NewGuid(),
            "إجابة", "ar", parent, expert, clock);

    [Fact]
    public void Create_top_level_reply()
    {
        var r = NewReply(NewClock());
        r.ParentReplyId.Should().BeNull();
        r.IsByExpert.Should().BeFalse();
    }

    [Fact]
    public void Create_threaded_reply_has_parent()
    {
        var parent = System.Guid.NewGuid();
        var r = NewReply(NewClock(), parent);
        r.ParentReplyId.Should().Be(parent);
    }

    [Fact]
    public void Expert_flag_persisted_at_creation()
    {
        var r = NewReply(NewClock(), null, expert: true);
        r.IsByExpert.Should().BeTrue();
    }

    [Fact]
    public void Content_over_8000_throws()
    {
        var clock = NewClock();
        var huge = new string('x', PostReply.MaxContentLength + 1);
        var act = () => PostReply.Create(System.Guid.NewGuid(), System.Guid.NewGuid(),
            huge, "ar", null, false, clock);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Invalid_locale_throws()
    {
        var clock = NewClock();
        var act = () => PostReply.Create(System.Guid.NewGuid(), System.Guid.NewGuid(),
            "x", "fr", null, false, clock);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void EditContent_replaces_text()
    {
        var r = NewReply(NewClock());
        r.EditContent("جديد");
        r.Content.Should().Be("جديد");
    }

    [Fact]
    public void SoftDelete_marks_deleted()
    {
        var r = NewReply(NewClock());
        r.SoftDelete(System.Guid.NewGuid(), NewClock());
        r.IsDeleted.Should().BeTrue();
    }
}
