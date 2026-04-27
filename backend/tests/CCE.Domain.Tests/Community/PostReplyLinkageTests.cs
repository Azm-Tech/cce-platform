using CCE.Domain.Community;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Community;

public class PostReplyLinkageTests
{
    [Fact]
    public void Replying_then_marking_as_answer_links_question_to_reply()
    {
        var clock = new FakeSystemClock();
        var question = Post.Create(System.Guid.NewGuid(), System.Guid.NewGuid(),
            "سؤال", "ar", isAnswerable: true, clock);
        var reply = PostReply.Create(question.Id, System.Guid.NewGuid(),
            "إجابة", "ar", null, isByExpert: true, clock);

        question.MarkAnswered(reply.Id);

        question.AnsweredReplyId.Should().Be(reply.Id);
        reply.PostId.Should().Be(question.Id);
        reply.IsByExpert.Should().BeTrue();
    }

    [Fact]
    public void Threaded_reply_chain_preserves_parent_links()
    {
        var clock = new FakeSystemClock();
        var post = Post.Create(System.Guid.NewGuid(), System.Guid.NewGuid(),
            "س", "ar", isAnswerable: false, clock);
        var top = PostReply.Create(post.Id, System.Guid.NewGuid(),
            "أ", "ar", null, isByExpert: false, clock);
        var nested = PostReply.Create(post.Id, System.Guid.NewGuid(),
            "ب", "ar", parentReplyId: top.Id, isByExpert: false, clock);

        nested.ParentReplyId.Should().Be(top.Id);
        top.ParentReplyId.Should().BeNull();
    }
}
