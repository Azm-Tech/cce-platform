using CCE.Application.Common.Interfaces;
using CCE.Application.Community.Public.Queries.ListPublicPostReplies;
using CCE.Domain.Common;
using CCE.Domain.Community;

namespace CCE.Application.Tests.Community.Public.Queries;

public class ListPublicPostRepliesQueryHandlerTests
{
    private static ISystemClock MakeClock()
    {
        var clock = Substitute.For<ISystemClock>();
        clock.UtcNow.Returns(System.DateTimeOffset.UtcNow);
        return clock;
    }

    [Fact]
    public async Task Returns_replies_for_post_only()
    {
        var clock = MakeClock();
        var postId = System.Guid.NewGuid();
        var authorId = System.Guid.NewGuid();
        var reply1 = PostReply.Create(postId, authorId, "Reply one", "en", null, false, clock);
        var reply2 = PostReply.Create(postId, authorId, "Reply two", "en", null, false, clock);
        var otherReply = PostReply.Create(System.Guid.NewGuid(), authorId, "Other post reply", "en", null, false, clock);

        var db = BuildDb(new[] { reply1, reply2, otherReply });
        var sut = new ListPublicPostRepliesQueryHandler(db);

        var result = await sut.Handle(new ListPublicPostRepliesQuery(postId, 1, 20), CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.Total.Should().Be(2);
        result.Items.Should().AllSatisfy(r => r.PostId.Should().Be(postId));
    }

    [Fact]
    public async Task Returns_empty_when_no_replies_for_post()
    {
        var db = BuildDb(System.Array.Empty<PostReply>());
        var sut = new ListPublicPostRepliesQueryHandler(db);

        var result = await sut.Handle(new ListPublicPostRepliesQuery(System.Guid.NewGuid(), 1, 20), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.Total.Should().Be(0);
    }

    [Fact]
    public async Task Maps_all_fields_correctly()
    {
        var clock = MakeClock();
        var postId = System.Guid.NewGuid();
        var authorId = System.Guid.NewGuid();
        var parentId = System.Guid.NewGuid();
        var reply = PostReply.Create(postId, authorId, "Expert answer", "ar", parentId, true, clock);
        var db = BuildDb(new[] { reply });
        var sut = new ListPublicPostRepliesQueryHandler(db);

        var result = await sut.Handle(new ListPublicPostRepliesQuery(postId, 1, 20), CancellationToken.None);

        result.Items.Should().HaveCount(1);
        var dto = result.Items[0];
        dto.AuthorId.Should().Be(authorId);
        dto.Content.Should().Be("Expert answer");
        dto.Locale.Should().Be("ar");
        dto.ParentReplyId.Should().Be(parentId);
        dto.IsByExpert.Should().BeTrue();
    }

    private static ICceDbContext BuildDb(IEnumerable<PostReply> replies)
    {
        var db = Substitute.For<ICceDbContext>();
        db.PostReplies.Returns(replies.AsQueryable());
        return db;
    }
}
