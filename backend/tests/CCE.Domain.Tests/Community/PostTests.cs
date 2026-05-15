using CCE.Domain.Common;
using CCE.Domain.Community;
using CCE.Domain.Community.Events;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Community;

public class PostTests
{
    private static FakeSystemClock NewClock() => new();

    private static Post NewQuestion(FakeSystemClock clock) =>
        Post.Create(System.Guid.NewGuid(), System.Guid.NewGuid(),
            "ما رأيكم في الطاقة الشمسية؟", "ar", isAnswerable: true, clock);

    [Fact]
    public void Create_question_post()
    {
        var p = NewQuestion(NewClock());
        p.IsAnswerable.Should().BeTrue();
        p.AnsweredReplyId.Should().BeNull();
        p.Locale.Should().Be("ar");
    }

    [Fact]
    public void Create_raises_PostCreatedEvent()
    {
        var p = NewQuestion(NewClock());
        p.DomainEvents.OfType<PostCreatedEvent>().Should().HaveCount(1);
    }

    [Fact]
    public void Create_with_invalid_locale_throws()
    {
        var clock = NewClock();
        var act = () => Post.Create(System.Guid.NewGuid(), System.Guid.NewGuid(), "x", "fr", false, clock);
        act.Should().Throw<DomainException>().WithMessage("*locale*");
    }

    [Fact]
    public void Content_exceeding_8000_chars_throws()
    {
        var clock = NewClock();
        var huge = new string('a', Post.MaxContentLength + 1);
        var act = () => Post.Create(System.Guid.NewGuid(), System.Guid.NewGuid(), huge, "ar", false, clock);
        act.Should().Throw<DomainException>().WithMessage("*8000*");
    }

    [Fact]
    public void MarkAnswered_on_question_sets_AnsweredReplyId()
    {
        var p = NewQuestion(NewClock());
        var reply = System.Guid.NewGuid();
        p.MarkAnswered(reply);
        p.AnsweredReplyId.Should().Be(reply);
    }

    [Fact]
    public void MarkAnswered_on_discussion_throws()
    {
        var clock = NewClock();
        var discussion = Post.Create(System.Guid.NewGuid(), System.Guid.NewGuid(), "x", "ar", false, clock);
        var act = () => discussion.MarkAnswered(System.Guid.NewGuid());
        act.Should().Throw<DomainException>().WithMessage("*answerable*");
    }

    [Fact]
    public void ClearAnswer_unsets_AnsweredReplyId()
    {
        var p = NewQuestion(NewClock());
        p.MarkAnswered(System.Guid.NewGuid());
        p.ClearAnswer();
        p.AnsweredReplyId.Should().BeNull();
    }

    [Fact]
    public void EditContent_updates_text()
    {
        var clock = NewClock();
        var p = NewQuestion(clock);
        var editor = System.Guid.NewGuid();
        clock.Advance(System.TimeSpan.FromMinutes(1));
        p.EditContent("نص جديد", editor, clock);
        p.Content.Should().Be("نص جديد");
        p.LastModifiedOn.Should().Be(clock.UtcNow);
        p.LastModifiedById.Should().Be(editor);
    }
}
