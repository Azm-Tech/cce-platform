using CCE.Application.Community.Queries.ListAdminPosts;
using CCE.Domain.Common;
using CCE.Domain.Community;
using CCE.Infrastructure.Persistence;
using CCE.TestInfrastructure.Time;
using Microsoft.EntityFrameworkCore;

namespace CCE.Application.Tests.Community.Queries;

/// <summary>
/// Covers <see cref="ListAdminPostsQueryHandler"/> using an EF Core in-memory database.
/// The handler relies on EF-specific operators (<c>IgnoreQueryFilters</c>, <c>EF.Functions.Like</c>,
/// async grouped sub-queries) so a substituted <c>ICceDbContext</c> with a plain in-memory
/// <c>IQueryable</c> is not enough — this fixture wires up the real <see cref="CceDbContext"/>
/// against the EF in-memory provider, which is what the sibling Assistant tests use.
/// </summary>
public sealed class ListAdminPostsQueryHandlerTests : IDisposable
{
    private readonly CceDbContext _db;
    private readonly FakeSystemClock _clock;
    private readonly ListAdminPostsQueryHandler _sut;

    public ListAdminPostsQueryHandlerTests()
    {
        var opts = new DbContextOptionsBuilder<CceDbContext>()
            .UseInMemoryDatabase($"list-admin-posts-{Guid.NewGuid()}")
            .Options;
        _db = new CceDbContext(opts);
        _clock = new FakeSystemClock();
        _sut = new ListAdminPostsQueryHandler(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Returns_active_and_deleted_when_status_all()
    {
        var (energy, _) = SeedTopics();
        var active = Post.Create(energy.Id, Guid.NewGuid(), "Active post", "en", false, _clock);
        var soft = Post.Create(energy.Id, Guid.NewGuid(), "Soft-deleted post", "en", false, _clock);
        soft.SoftDelete(Guid.NewGuid(), _clock);
        _db.Posts.AddRange(active, soft);
        await _db.SaveChangesAsync();

        var result = await _sut.Handle(new ListAdminPostsQuery(Status: "all"), CancellationToken.None);

        result.Total.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items.Select(r => r.IsDeleted).Should().BeEquivalentTo(new[] { true, false });
    }

    [Fact]
    public async Task Filters_by_status_deleted()
    {
        var (energy, _) = SeedTopics();
        var active = Post.Create(energy.Id, Guid.NewGuid(), "still here", "en", false, _clock);
        var soft = Post.Create(energy.Id, Guid.NewGuid(), "gone", "en", false, _clock);
        soft.SoftDelete(Guid.NewGuid(), _clock);
        _db.Posts.AddRange(active, soft);
        await _db.SaveChangesAsync();

        var result = await _sut.Handle(new ListAdminPostsQuery(Status: "deleted"), CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items.Should().ContainSingle().Which.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Filters_by_status_question_excludes_answered_and_discussion()
    {
        var (energy, _) = SeedTopics();
        var question = Post.Create(energy.Id, Guid.NewGuid(), "open?", "en", isAnswerable: true, _clock);
        var answered = Post.Create(energy.Id, Guid.NewGuid(), "closed?", "en", isAnswerable: true, _clock);
        answered.MarkAnswered(Guid.NewGuid());
        var discussion = Post.Create(energy.Id, Guid.NewGuid(), "chat", "en", isAnswerable: false, _clock);
        _db.Posts.AddRange(question, answered, discussion);
        await _db.SaveChangesAsync();

        var result = await _sut.Handle(new ListAdminPostsQuery(Status: "question"), CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items.Should().ContainSingle().Which.IsAnswerable.Should().BeTrue();
        result.Items[0].IsAnswered.Should().BeFalse();
    }

    [Fact]
    public async Task Filters_by_status_answered()
    {
        var (energy, _) = SeedTopics();
        var open = Post.Create(energy.Id, Guid.NewGuid(), "open", "en", true, _clock);
        var done = Post.Create(energy.Id, Guid.NewGuid(), "done", "en", true, _clock);
        done.MarkAnswered(Guid.NewGuid());
        _db.Posts.AddRange(open, done);
        await _db.SaveChangesAsync();

        var result = await _sut.Handle(new ListAdminPostsQuery(Status: "answered"), CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items[0].IsAnswered.Should().BeTrue();
    }

    [Fact]
    public async Task Filters_by_topic_id()
    {
        var (energy, policy) = SeedTopics();
        var eP = Post.Create(energy.Id, Guid.NewGuid(), "in energy", "en", false, _clock);
        var pP = Post.Create(policy.Id, Guid.NewGuid(), "in policy", "en", false, _clock);
        _db.Posts.AddRange(eP, pP);
        await _db.SaveChangesAsync();

        var result = await _sut.Handle(new ListAdminPostsQuery(TopicId: policy.Id), CancellationToken.None);

        result.Items.Should().ContainSingle().Which.TopicId.Should().Be(policy.Id);
    }

    [Fact]
    public async Task Filters_by_locale()
    {
        var (energy, _) = SeedTopics();
        var en = Post.Create(energy.Id, Guid.NewGuid(), "english", "en", false, _clock);
        var ar = Post.Create(energy.Id, Guid.NewGuid(), "عربي", "ar", false, _clock);
        _db.Posts.AddRange(en, ar);
        await _db.SaveChangesAsync();

        var result = await _sut.Handle(new ListAdminPostsQuery(Locale: "ar"), CancellationToken.None);

        result.Items.Should().ContainSingle().Which.Locale.Should().Be("ar");
    }

    [Fact]
    public async Task Ignores_unknown_locale_filter()
    {
        var (energy, _) = SeedTopics();
        _db.Posts.Add(Post.Create(energy.Id, Guid.NewGuid(), "post", "en", false, _clock));
        await _db.SaveChangesAsync();

        var result = await _sut.Handle(new ListAdminPostsQuery(Locale: "fr"), CancellationToken.None);

        result.Total.Should().Be(1);
    }

    [Fact]
    public async Task Orders_by_created_on_descending()
    {
        var (energy, _) = SeedTopics();
        var older = Post.Create(energy.Id, Guid.NewGuid(), "older", "en", false, _clock);
        _db.Posts.Add(older);
        await _db.SaveChangesAsync();

        _clock.Advance(TimeSpan.FromMinutes(5));
        var newer = Post.Create(energy.Id, Guid.NewGuid(), "newer", "en", false, _clock);
        _db.Posts.Add(newer);
        await _db.SaveChangesAsync();

        var result = await _sut.Handle(new ListAdminPostsQuery(), CancellationToken.None);

        result.Items[0].Content.Should().Be("newer");
        result.Items[1].Content.Should().Be("older");
    }

    [Fact]
    public async Task Joins_topic_names_and_reply_count()
    {
        var (energy, _) = SeedTopics();
        var post = Post.Create(energy.Id, Guid.NewGuid(), "Q", "en", false, _clock);
        var reply1 = PostReply.Create(post.Id, Guid.NewGuid(), "r1", "en", null, false, _clock);
        var reply2 = PostReply.Create(post.Id, Guid.NewGuid(), "r2", "en", null, false, _clock);
        _db.Posts.Add(post);
        _db.PostReplies.AddRange(reply1, reply2);
        await _db.SaveChangesAsync();

        var result = await _sut.Handle(new ListAdminPostsQuery(), CancellationToken.None);

        var row = result.Items.Should().ContainSingle().Subject;
        row.TopicNameEn.Should().Be("Energy");
        row.TopicNameAr.Should().Be("طاقة");
        row.ReplyCount.Should().Be(2);
    }

    [Fact]
    public async Task Paginates_clamping_and_total()
    {
        var (energy, _) = SeedTopics();
        for (var i = 0; i < 5; i++)
        {
            _db.Posts.Add(Post.Create(energy.Id, Guid.NewGuid(), $"p{i}", "en", false, _clock));
            _clock.Advance(TimeSpan.FromSeconds(1));
        }
        await _db.SaveChangesAsync();

        var page1 = await _sut.Handle(new ListAdminPostsQuery(Page: 1, PageSize: 2), CancellationToken.None);
        var page2 = await _sut.Handle(new ListAdminPostsQuery(Page: 2, PageSize: 2), CancellationToken.None);

        page1.Total.Should().Be(5);
        page1.Items.Should().HaveCount(2);
        page2.Items.Should().HaveCount(2);
        page1.Items.Select(r => r.Id).Should().NotIntersectWith(page2.Items.Select(r => r.Id));
    }

    [Fact]
    public async Task Returns_empty_page_with_total_when_no_matches()
    {
        var (energy, _) = SeedTopics();
        _db.Posts.Add(Post.Create(energy.Id, Guid.NewGuid(), "post", "en", false, _clock));
        await _db.SaveChangesAsync();

        var result = await _sut.Handle(new ListAdminPostsQuery(TopicId: Guid.NewGuid()), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.Total.Should().Be(0);
        result.Page.Should().Be(1);
    }

    private (Topic energy, Topic policy) SeedTopics()
    {
        var energy = Topic.Create("طاقة", "Energy", "وصف", "Energy description", "energy", null, null, 1);
        var policy = Topic.Create("سياسات", "Policy", "وصف", "Policy description", "policy", null, null, 2);
        _db.Topics.AddRange(energy, policy);
        _db.SaveChanges();
        return (energy, policy);
    }
}
