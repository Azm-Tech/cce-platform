using CCE.Application.Common.Interfaces;
using CCE.Application.Community.Public.Queries.ListPublicPostsInTopic;
using CCE.Domain.Common;
using CCE.Domain.Community;

namespace CCE.Application.Tests.Community.Public.Queries;

public class ListPublicPostsInTopicQueryHandlerTests
{
    private static ISystemClock MakeClock()
    {
        var clock = Substitute.For<ISystemClock>();
        clock.UtcNow.Returns(System.DateTimeOffset.UtcNow);
        return clock;
    }

    [Fact]
    public async Task Returns_posts_for_topic_only()
    {
        var clock = MakeClock();
        var topicId = System.Guid.NewGuid();
        var authorId = System.Guid.NewGuid();
        var post1 = Post.Create(topicId, authorId, "First post", "en", false, clock);
        var post2 = Post.Create(topicId, authorId, "Second post", "en", false, clock);
        var otherTopicPost = Post.Create(System.Guid.NewGuid(), authorId, "Other topic", "en", false, clock);

        var db = BuildDb(new[] { post1, post2, otherTopicPost });
        var sut = new ListPublicPostsInTopicQueryHandler(db);

        var result = await sut.Handle(new ListPublicPostsInTopicQuery(topicId, 1, 20), CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.Total.Should().Be(2);
        result.Items.Should().AllSatisfy(p => p.TopicId.Should().Be(topicId));
    }

    [Fact]
    public async Task Returns_empty_when_no_posts_in_topic()
    {
        var topicId = System.Guid.NewGuid();
        var db = BuildDb(System.Array.Empty<Post>());
        var sut = new ListPublicPostsInTopicQueryHandler(db);

        var result = await sut.Handle(new ListPublicPostsInTopicQuery(topicId, 1, 20), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.Total.Should().Be(0);
    }

    [Fact]
    public async Task Paginates_correctly()
    {
        var clock = MakeClock();
        var topicId = System.Guid.NewGuid();
        var authorId = System.Guid.NewGuid();
        var posts = Enumerable.Range(1, 5)
            .Select(_ => Post.Create(topicId, authorId, "content", "en", false, clock))
            .ToArray();

        var db = BuildDb(posts);
        var sut = new ListPublicPostsInTopicQueryHandler(db);

        var result = await sut.Handle(new ListPublicPostsInTopicQuery(topicId, 1, 3), CancellationToken.None);

        result.Items.Should().HaveCount(3);
        result.Total.Should().Be(5);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(3);
    }

    private static ICceDbContext BuildDb(IEnumerable<Post> posts)
    {
        var db = Substitute.For<ICceDbContext>();
        db.Posts.Returns(posts.AsQueryable());
        return db;
    }
}
