using CCE.Domain.Common;
using CCE.Domain.Community;
using CCE.Domain.Community.Events;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Community;

public class PostTests
{
    private static FakeSystemClock NewClock() => new();

    private static Post NewQuestion(FakeSystemClock clock) =>
        Post.CreateDraft(System.Guid.NewGuid(), System.Guid.NewGuid(), System.Guid.NewGuid(),
            PostType.Question, "عنوان", "ما رأيكم في الطاقة الشمسية؟", "ar", clock);

    [Fact]
    public void Question_draft_is_answerable()
    {
        var p = NewQuestion(NewClock());
        p.IsAnswerable.Should().BeTrue();
        p.Status.Should().Be(PostStatus.Draft);
        p.AnsweredReplyId.Should().BeNull();
        p.Locale.Should().Be("ar");
    }

    [Fact]
    public void CreateDraft_does_not_raise_PostCreatedEvent()
    {
        var p = NewQuestion(NewClock());
        p.DomainEvents.OfType<PostCreatedEvent>().Should().BeEmpty();
    }

    [Fact]
    public void Publish_raises_PostCreatedEvent_once_and_is_idempotent()
    {
        var p = NewQuestion(NewClock());
        p.Publish(NewClock());
        p.Publish(NewClock());
        p.Status.Should().Be(PostStatus.Published);
        p.PublishedOn.Should().NotBeNull();
        p.DomainEvents.OfType<PostCreatedEvent>().Should().HaveCount(1);
    }

    [Fact]
    public void Publish_without_title_throws()
    {
        var clock = NewClock();
        var draft = Post.CreateDraft(System.Guid.NewGuid(), System.Guid.NewGuid(), System.Guid.NewGuid(),
            PostType.Info, title: null, content: "body", "ar", clock);
        var act = () => draft.Publish(clock);
        act.Should().Throw<DomainException>().WithMessage("*Title*");
    }

    [Fact]
    public void CreateDraft_with_invalid_locale_throws()
    {
        var clock = NewClock();
        var act = () => Post.CreateDraft(System.Guid.NewGuid(), System.Guid.NewGuid(), System.Guid.NewGuid(),
            PostType.Info, "t", "x", "fr", clock);
        act.Should().Throw<DomainException>().WithMessage("*locale*");
    }

    [Fact]
    public void Content_exceeding_8000_chars_throws()
    {
        var clock = NewClock();
        var huge = new string('a', Post.MaxContentLength + 1);
        var act = () => Post.CreateDraft(System.Guid.NewGuid(), System.Guid.NewGuid(), System.Guid.NewGuid(),
            PostType.Info, "t", huge, "ar", clock);
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
        var discussion = Post.CreateDraft(System.Guid.NewGuid(), System.Guid.NewGuid(), System.Guid.NewGuid(),
            PostType.Info, "t", "x", "ar", clock);
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
