using CCE.Domain.Community;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Community;

public class PostReplyLinkageTests
{
    [Fact]
    public void Replying_then_marking_as_answer_links_question_to_reply()
    {
        var clock = new FakeSystemClock();
        var question = Post.CreateDraft(System.Guid.NewGuid(), System.Guid.NewGuid(), System.Guid.NewGuid(),
            PostType.Question, "عنوان", "سؤال", "ar", clock);
        var reply = PostReply.CreateRoot(question.Id, System.Guid.NewGuid(),
            "إجابة", "ar", isByExpert: true, clock);

        question.MarkAnswered(reply.Id);

        question.AnsweredReplyId.Should().Be(reply.Id);
        reply.PostId.Should().Be(question.Id);
        reply.IsByExpert.Should().BeTrue();
    }

    [Fact]
    public void Threaded_reply_chain_preserves_parent_links()
    {
        var clock = new FakeSystemClock();
        var post = Post.CreateDraft(System.Guid.NewGuid(), System.Guid.NewGuid(), System.Guid.NewGuid(),
            PostType.Info, "عنوان", "س", "ar", clock);
        var top = PostReply.CreateRoot(post.Id, System.Guid.NewGuid(),
            "أ", "ar", isByExpert: false, clock);
        var nested = PostReply.CreateChild(top, System.Guid.NewGuid(),
            "ب", "ar", isByExpert: false, clock);

        nested.ParentReplyId.Should().Be(top.Id);
        nested.Depth.Should().Be(1);
        nested.ThreadPath.Should().StartWith(top.ThreadPath);
        top.ParentReplyId.Should().BeNull();
        top.ChildCount.Should().Be(1);
    }
}
