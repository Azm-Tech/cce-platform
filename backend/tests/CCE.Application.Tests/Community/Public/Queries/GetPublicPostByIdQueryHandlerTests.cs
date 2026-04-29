using CCE.Application.Common.Interfaces;
using CCE.Application.Community.Public.Queries.GetPublicPostById;
using CCE.Domain.Common;
using CCE.Domain.Community;

namespace CCE.Application.Tests.Community.Public.Queries;

public class GetPublicPostByIdQueryHandlerTests
{
    private static ISystemClock MakeClock()
    {
        var clock = Substitute.For<ISystemClock>();
        clock.UtcNow.Returns(System.DateTimeOffset.UtcNow);
        return clock;
    }

    [Fact]
    public async Task Returns_dto_when_post_exists()
    {
        var clock = MakeClock();
        var post = Post.Create(System.Guid.NewGuid(), System.Guid.NewGuid(), "Hello world", "en", false, clock);
        var db = BuildDb(new[] { post });
        var sut = new GetPublicPostByIdQueryHandler(db);

        var result = await sut.Handle(new GetPublicPostByIdQuery(post.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(post.Id);
        result.Content.Should().Be("Hello world");
    }

    [Fact]
    public async Task Returns_null_when_post_not_found()
    {
        var db = BuildDb(System.Array.Empty<Post>());
        var sut = new GetPublicPostByIdQueryHandler(db);

        var result = await sut.Handle(new GetPublicPostByIdQuery(System.Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Maps_all_fields_correctly()
    {
        var clock = MakeClock();
        var topicId = System.Guid.NewGuid();
        var authorId = System.Guid.NewGuid();
        var post = Post.Create(topicId, authorId, "Test content", "ar", true, clock);
        var db = BuildDb(new[] { post });
        var sut = new GetPublicPostByIdQueryHandler(db);

        var result = await sut.Handle(new GetPublicPostByIdQuery(post.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.TopicId.Should().Be(topicId);
        result.AuthorId.Should().Be(authorId);
        result.Locale.Should().Be("ar");
        result.IsAnswerable.Should().BeTrue();
        result.AnsweredReplyId.Should().BeNull();
    }

    private static ICceDbContext BuildDb(IEnumerable<Post> posts)
    {
        var db = Substitute.For<ICceDbContext>();
        db.Posts.Returns(posts.AsQueryable());
        return db;
    }
}
