using CCE.Domain.Common;
using CCE.Domain.Community;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Community;

public class PostAttachmentTests
{
    private static Post NewPost()
        => Post.CreateDraft(System.Guid.NewGuid(), System.Guid.NewGuid(), System.Guid.NewGuid(),
            PostType.Info, "Title", "Body", "en", new FakeSystemClock());

    [Fact]
    public void AddAttachment_appends_to_collection()
    {
        var post = NewPost();
        post.AddAttachment(System.Guid.NewGuid(), AttachmentKind.Media, 0, null);
        post.AddAttachment(System.Guid.NewGuid(), AttachmentKind.Document, 1, "{\"caption\":\"x\"}");
        post.Attachments.Should().HaveCount(2);
    }

    [Fact]
    public void AddAttachment_beyond_cap_throws()
    {
        var post = NewPost();
        for (var i = 0; i < Post.MaxAttachments; i++)
            post.AddAttachment(System.Guid.NewGuid(), AttachmentKind.Media, i, null);

        var act = () => post.AddAttachment(System.Guid.NewGuid(), AttachmentKind.Media, 99, null);
        act.Should().Throw<DomainException>().WithMessage("*at most*");
    }

    [Fact]
    public void PostAttachment_requires_asset_id()
    {
        var act = () => PostAttachment.Create(System.Guid.NewGuid(), System.Guid.Empty, AttachmentKind.Media, 0, null);
        act.Should().Throw<DomainException>();
    }
}
