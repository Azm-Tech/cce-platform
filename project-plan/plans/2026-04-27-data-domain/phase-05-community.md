# Phase 05 — Community bounded context

> Parent: [`../2026-04-27-data-domain.md`](../2026-04-27-data-domain.md) · Spec: [`../../specs/2026-04-27-data-domain-design.md`](../../specs/2026-04-27-data-domain-design.md) §4.4

**Phase goal:** Land 7 Community entities under `CCE.Domain.Community/`: `Topic` (hierarchical), `Post` (aggregate root), `PostReply` (threaded), `PostRating`, and three follow associations (`TopicFollow`, `UserFollow`, `PostFollow`). Pure domain layer.

**Tasks in this phase:** 10
**Working directory:** `/Users/m/CCE/`
**Preconditions:**
- Phase 04 closed (`551f403` is HEAD).
- 233 backend tests passing.

---

## Task 5.1: `Topic` entity (hierarchical)

**Files:**
- Create: `backend/src/CCE.Domain/Community/Topic.cs`
- Create: `backend/tests/CCE.Domain.Tests/Community/TopicTests.cs`

Single-language posts live under topics; topics themselves are bilingual.

- [ ] **Step 1: Entity**

```csharp
using System.Text.RegularExpressions;
using CCE.Domain.Common;

namespace CCE.Domain.Community;

[Audited]
public sealed class Topic : Entity<System.Guid>, ISoftDeletable
{
    private static readonly Regex SlugPattern = new("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);

    private Topic(
        System.Guid id, string nameAr, string nameEn,
        string descriptionAr, string descriptionEn,
        string slug, System.Guid? parentId,
        string? iconUrl, int orderIndex) : base(id)
    {
        NameAr = nameAr; NameEn = nameEn;
        DescriptionAr = descriptionAr; DescriptionEn = descriptionEn;
        Slug = slug; ParentId = parentId;
        IconUrl = iconUrl; OrderIndex = orderIndex;
        IsActive = true;
    }

    public string NameAr { get; private set; }
    public string NameEn { get; private set; }
    public string DescriptionAr { get; private set; }
    public string DescriptionEn { get; private set; }
    public string Slug { get; private set; }
    public System.Guid? ParentId { get; private set; }
    public string? IconUrl { get; private set; }
    public int OrderIndex { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }
    public System.DateTimeOffset? DeletedOn { get; private set; }
    public System.Guid? DeletedById { get; private set; }

    public static Topic Create(
        string nameAr, string nameEn,
        string descriptionAr, string descriptionEn,
        string slug, System.Guid? parentId,
        string? iconUrl, int orderIndex)
    {
        if (string.IsNullOrWhiteSpace(nameAr)) throw new DomainException("NameAr is required.");
        if (string.IsNullOrWhiteSpace(nameEn)) throw new DomainException("NameEn is required.");
        if (string.IsNullOrWhiteSpace(descriptionAr)) throw new DomainException("DescriptionAr is required.");
        if (string.IsNullOrWhiteSpace(descriptionEn)) throw new DomainException("DescriptionEn is required.");
        if (string.IsNullOrWhiteSpace(slug) || !SlugPattern.IsMatch(slug))
        {
            throw new DomainException($"slug '{slug}' must be kebab-case.");
        }
        if (iconUrl is not null
            && !iconUrl.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("IconUrl must use https://.");
        }
        return new Topic(System.Guid.NewGuid(), nameAr, nameEn,
            descriptionAr, descriptionEn, slug, parentId, iconUrl, orderIndex);
    }

    public void UpdateContent(string nameAr, string nameEn, string descriptionAr, string descriptionEn)
    {
        if (string.IsNullOrWhiteSpace(nameAr)) throw new DomainException("NameAr is required.");
        if (string.IsNullOrWhiteSpace(nameEn)) throw new DomainException("NameEn is required.");
        if (string.IsNullOrWhiteSpace(descriptionAr)) throw new DomainException("DescriptionAr is required.");
        if (string.IsNullOrWhiteSpace(descriptionEn)) throw new DomainException("DescriptionEn is required.");
        NameAr = nameAr; NameEn = nameEn;
        DescriptionAr = descriptionAr; DescriptionEn = descriptionEn;
    }

    public void Reorder(int orderIndex) => OrderIndex = orderIndex;

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    public void SoftDelete(System.Guid deletedById, ISystemClock clock)
    {
        if (deletedById == System.Guid.Empty) throw new DomainException("DeletedById is required.");
        if (IsDeleted) return;
        IsDeleted = true;
        DeletedById = deletedById;
        DeletedOn = clock.UtcNow;
    }
}
```

- [ ] **Step 2: Tests**

```csharp
using CCE.Domain.Common;
using CCE.Domain.Community;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Community;

public class TopicTests
{
    private static Topic NewRoot() => Topic.Create(
        "أساسيات", "Basics", "ا", "Description", "basics", null, null, 0);

    [Fact]
    public void Create_root_topic_is_active()
    {
        var t = NewRoot();
        t.IsActive.Should().BeTrue();
        t.ParentId.Should().BeNull();
    }

    [Fact]
    public void Create_child_topic_has_parent()
    {
        var parent = System.Guid.NewGuid();
        var t = Topic.Create("ا", "x", "ا", "x", "child", parent, null, 0);
        t.ParentId.Should().Be(parent);
    }

    [Fact]
    public void Slug_must_be_kebab_case()
    {
        var act = () => Topic.Create("ا", "x", "ا", "x", "Bad Slug", null, null, 0);
        act.Should().Throw<DomainException>().WithMessage("*slug*");
    }

    [Fact]
    public void IconUrl_must_be_https()
    {
        var act = () => Topic.Create("ا", "x", "ا", "x", "x", null, "http://insecure", 0);
        act.Should().Throw<DomainException>().WithMessage("*Icon*");
    }

    [Fact]
    public void UpdateContent_replaces_bilingual_fields()
    {
        var t = NewRoot();
        t.UpdateContent("ج", "new", "ج", "new");
        t.NameEn.Should().Be("new");
        t.DescriptionAr.Should().Be("ج");
    }

    [Fact]
    public void Deactivate_then_Activate_toggles()
    {
        var t = NewRoot();
        t.Deactivate();
        t.IsActive.Should().BeFalse();
        t.Activate();
        t.IsActive.Should().BeTrue();
    }

    [Fact]
    public void SoftDelete_marks_deleted()
    {
        var t = NewRoot();
        t.SoftDelete(System.Guid.NewGuid(), new FakeSystemClock());
        t.IsDeleted.Should().BeTrue();
    }
}
```

- [ ] **Step 3: Run + commit**

```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~TopicTests" --logger "console;verbosity=minimal" 2>&1 | tail -3
git add backend/src/CCE.Domain/Community/Topic.cs backend/tests/CCE.Domain.Tests/Community/TopicTests.cs
git -c commit.gpgsign=false commit -m "feat(community): Topic hierarchical entity (7 TDD tests)"
```

---

## Task 5.2: `Post` aggregate (8000-char limit + answer marking)

**Files:**
- Create: `backend/src/CCE.Domain/Community/Events/PostCreatedEvent.cs`
- Create: `backend/src/CCE.Domain/Community/Post.cs`
- Create: `backend/tests/CCE.Domain.Tests/Community/PostTests.cs`

- [ ] **Step 1: Event**

```csharp
using CCE.Domain.Common;

namespace CCE.Domain.Community.Events;

public sealed record PostCreatedEvent(
    System.Guid PostId,
    System.Guid TopicId,
    System.Guid AuthorId,
    string Locale,
    System.DateTimeOffset OccurredOn) : IDomainEvent;
```

- [ ] **Step 2: Aggregate**

```csharp
using CCE.Domain.Common;
using CCE.Domain.Community.Events;

namespace CCE.Domain.Community;

/// <summary>
/// Community post (question or discussion). Single-language: the author writes in their
/// own language and the entity records that locale. Question posts (<see cref="IsAnswerable"/>=true)
/// can have a <see cref="AnsweredReplyId"/> — set by the asker when they accept a reply as the answer.
/// Content max 8000 chars to keep the read-side cheap.
/// </summary>
[Audited]
public sealed class Post : AggregateRoot<System.Guid>, ISoftDeletable
{
    public const int MaxContentLength = 8000;

    private Post(
        System.Guid id,
        System.Guid topicId,
        System.Guid authorId,
        string content,
        string locale,
        bool isAnswerable,
        System.DateTimeOffset createdOn) : base(id)
    {
        TopicId = topicId;
        AuthorId = authorId;
        Content = content;
        Locale = locale;
        IsAnswerable = isAnswerable;
        CreatedOn = createdOn;
    }

    public System.Guid TopicId { get; private set; }
    public System.Guid AuthorId { get; private set; }
    public string Content { get; private set; }
    public string Locale { get; private set; }
    public bool IsAnswerable { get; private set; }
    public System.Guid? AnsweredReplyId { get; private set; }
    public System.DateTimeOffset CreatedOn { get; private set; }
    public bool IsDeleted { get; private set; }
    public System.DateTimeOffset? DeletedOn { get; private set; }
    public System.Guid? DeletedById { get; private set; }

    public static Post Create(
        System.Guid topicId,
        System.Guid authorId,
        string content,
        string locale,
        bool isAnswerable,
        ISystemClock clock)
    {
        if (topicId == System.Guid.Empty) throw new DomainException("TopicId is required.");
        if (authorId == System.Guid.Empty) throw new DomainException("AuthorId is required.");
        if (string.IsNullOrWhiteSpace(content)) throw new DomainException("Content is required.");
        if (content.Length > MaxContentLength)
        {
            throw new DomainException($"Content exceeds {MaxContentLength} chars (got {content.Length}).");
        }
        if (locale != "ar" && locale != "en")
        {
            throw new DomainException("locale must be 'ar' or 'en'.");
        }
        var p = new Post(System.Guid.NewGuid(), topicId, authorId, content, locale, isAnswerable, clock.UtcNow);
        p.RaiseDomainEvent(new PostCreatedEvent(p.Id, topicId, authorId, locale, p.CreatedOn));
        return p;
    }

    public void MarkAnswered(System.Guid replyId)
    {
        if (!IsAnswerable)
        {
            throw new DomainException("Only answerable (question) posts can be marked answered.");
        }
        if (replyId == System.Guid.Empty) throw new DomainException("ReplyId is required.");
        AnsweredReplyId = replyId;
    }

    public void ClearAnswer() => AnsweredReplyId = null;

    public void EditContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) throw new DomainException("Content is required.");
        if (content.Length > MaxContentLength)
        {
            throw new DomainException($"Content exceeds {MaxContentLength} chars (got {content.Length}).");
        }
        Content = content;
    }

    public void SoftDelete(System.Guid deletedById, ISystemClock clock)
    {
        if (deletedById == System.Guid.Empty) throw new DomainException("DeletedById is required.");
        if (IsDeleted) return;
        IsDeleted = true;
        DeletedById = deletedById;
        DeletedOn = clock.UtcNow;
    }
}
```

- [ ] **Step 3: Tests**

```csharp
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
        var p = NewQuestion(NewClock());
        p.EditContent("نص جديد");
        p.Content.Should().Be("نص جديد");
    }
}
```

- [ ] **Step 4: Run + commit**

```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~Community.PostTests" --logger "console;verbosity=minimal" 2>&1 | tail -3
git add backend/src/CCE.Domain/Community/Events/PostCreatedEvent.cs backend/src/CCE.Domain/Community/Post.cs backend/tests/CCE.Domain.Tests/Community/PostTests.cs
git -c commit.gpgsign=false commit -m "feat(community): Post aggregate with 8000-char limit + answer-marking + Created event (8 TDD tests)"
```

---

## Task 5.3: `PostReply` (threaded)

**Files:**
- Create: `backend/src/CCE.Domain/Community/PostReply.cs`
- Create: `backend/tests/CCE.Domain.Tests/Community/PostReplyTests.cs`

Threaded by `ParentReplyId`. `IsByExpert` is denormalized at creation (caller passes the boolean).

- [ ] **Step 1: Entity**

```csharp
using CCE.Domain.Common;

namespace CCE.Domain.Community;

[Audited]
public sealed class PostReply : Entity<System.Guid>, ISoftDeletable
{
    public const int MaxContentLength = 8000;

    private PostReply(
        System.Guid id, System.Guid postId, System.Guid authorId,
        string content, string locale, System.Guid? parentReplyId,
        bool isByExpert, System.DateTimeOffset createdOn) : base(id)
    {
        PostId = postId; AuthorId = authorId;
        Content = content; Locale = locale;
        ParentReplyId = parentReplyId; IsByExpert = isByExpert;
        CreatedOn = createdOn;
    }

    public System.Guid PostId { get; private set; }
    public System.Guid AuthorId { get; private set; }
    public string Content { get; private set; }
    public string Locale { get; private set; }
    public System.Guid? ParentReplyId { get; private set; }
    public bool IsByExpert { get; private set; }
    public System.DateTimeOffset CreatedOn { get; private set; }
    public bool IsDeleted { get; private set; }
    public System.DateTimeOffset? DeletedOn { get; private set; }
    public System.Guid? DeletedById { get; private set; }

    public static PostReply Create(
        System.Guid postId, System.Guid authorId,
        string content, string locale, System.Guid? parentReplyId,
        bool isByExpert, ISystemClock clock)
    {
        if (postId == System.Guid.Empty) throw new DomainException("PostId is required.");
        if (authorId == System.Guid.Empty) throw new DomainException("AuthorId is required.");
        if (string.IsNullOrWhiteSpace(content)) throw new DomainException("Content is required.");
        if (content.Length > MaxContentLength)
        {
            throw new DomainException($"Content exceeds {MaxContentLength} chars.");
        }
        if (locale != "ar" && locale != "en")
        {
            throw new DomainException("locale must be 'ar' or 'en'.");
        }
        return new PostReply(System.Guid.NewGuid(), postId, authorId,
            content, locale, parentReplyId, isByExpert, clock.UtcNow);
    }

    public void EditContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) throw new DomainException("Content is required.");
        if (content.Length > MaxContentLength)
        {
            throw new DomainException($"Content exceeds {MaxContentLength} chars.");
        }
        Content = content;
    }

    public void SoftDelete(System.Guid deletedById, ISystemClock clock)
    {
        if (deletedById == System.Guid.Empty) throw new DomainException("DeletedById is required.");
        if (IsDeleted) return;
        IsDeleted = true;
        DeletedById = deletedById;
        DeletedOn = clock.UtcNow;
    }
}
```

- [ ] **Step 2: Tests**

```csharp
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
```

- [ ] **Step 3: Run + commit**

```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~PostReplyTests" --logger "console;verbosity=minimal" 2>&1 | tail -3
git add backend/src/CCE.Domain/Community/PostReply.cs backend/tests/CCE.Domain.Tests/Community/PostReplyTests.cs
git -c commit.gpgsign=false commit -m "feat(community): PostReply (threaded) with 8000-char limit + IsByExpert flag (7 TDD tests)"
```

---

## Task 5.4: `PostRating` (1–5 stars, unique per user/post)

**Files:**
- Create: `backend/src/CCE.Domain/Community/PostRating.cs`
- Create: `backend/tests/CCE.Domain.Tests/Community/PostRatingTests.cs`

NOT audited per spec §4.11 (high-volume association).

- [ ] **Step 1: Entity**

```csharp
using CCE.Domain.Common;

namespace CCE.Domain.Community;

/// <summary>
/// One user's star rating on a post (1–5). Uniqueness is enforced by Phase 08 unique
/// index on (PostId, UserId). NOT audited (high-volume association — spec §4.11).
/// </summary>
public sealed class PostRating : Entity<System.Guid>
{
    private PostRating(System.Guid id, System.Guid postId, System.Guid userId,
        int stars, System.DateTimeOffset ratedOn) : base(id)
    {
        PostId = postId; UserId = userId;
        Stars = stars; RatedOn = ratedOn;
    }

    public System.Guid PostId { get; private set; }
    public System.Guid UserId { get; private set; }
    public int Stars { get; private set; }
    public System.DateTimeOffset RatedOn { get; private set; }

    public static PostRating Rate(System.Guid postId, System.Guid userId, int stars, ISystemClock clock)
    {
        if (postId == System.Guid.Empty) throw new DomainException("PostId is required.");
        if (userId == System.Guid.Empty) throw new DomainException("UserId is required.");
        if (stars < 1 || stars > 5)
        {
            throw new DomainException($"Stars must be between 1 and 5 (got {stars}).");
        }
        return new PostRating(System.Guid.NewGuid(), postId, userId, stars, clock.UtcNow);
    }

    public void Update(int stars, ISystemClock clock)
    {
        if (stars < 1 || stars > 5)
        {
            throw new DomainException($"Stars must be between 1 and 5 (got {stars}).");
        }
        Stars = stars;
        RatedOn = clock.UtcNow;
    }
}
```

- [ ] **Step 2: Tests**

```csharp
using CCE.Domain.Common;
using CCE.Domain.Community;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Community;

public class PostRatingTests
{
    [Fact]
    public void Rate_with_valid_stars()
    {
        var clock = new FakeSystemClock();
        var r = PostRating.Rate(System.Guid.NewGuid(), System.Guid.NewGuid(), 4, clock);
        r.Stars.Should().Be(4);
        r.RatedOn.Should().Be(clock.UtcNow);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public void Rate_with_out_of_range_throws(int stars)
    {
        var clock = new FakeSystemClock();
        var act = () => PostRating.Rate(System.Guid.NewGuid(), System.Guid.NewGuid(), stars, clock);
        act.Should().Throw<DomainException>().WithMessage("*Stars*");
    }

    [Fact]
    public void Update_replaces_stars_and_ratedOn()
    {
        var clock = new FakeSystemClock();
        var r = PostRating.Rate(System.Guid.NewGuid(), System.Guid.NewGuid(), 3, clock);
        clock.Advance(System.TimeSpan.FromHours(1));

        r.Update(5, clock);

        r.Stars.Should().Be(5);
        r.RatedOn.Should().Be(clock.UtcNow);
    }

    [Fact]
    public void PostRating_is_NOT_audited()
    {
        var attrs = typeof(PostRating).GetCustomAttributes(typeof(AuditedAttribute), inherit: false);
        attrs.Should().BeEmpty(because: "high-volume association per spec §4.11");
    }
}
```

- [ ] **Step 3: Run + commit**

```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~PostRatingTests" --logger "console;verbosity=minimal" 2>&1 | tail -3
git add backend/src/CCE.Domain/Community/PostRating.cs backend/tests/CCE.Domain.Tests/Community/PostRatingTests.cs
git -c commit.gpgsign=false commit -m "feat(community): PostRating (1-5 stars, non-audited per §4.11) (6 TDD tests)"
```

---

## Task 5.5: `TopicFollow` association

**Files:**
- Create: `backend/src/CCE.Domain/Community/TopicFollow.cs`
- Create: `backend/tests/CCE.Domain.Tests/Community/TopicFollowTests.cs`

- [ ] **Step 1: Entity**

```csharp
using CCE.Domain.Common;

namespace CCE.Domain.Community;

/// <summary>User-follows-topic association. Unique (TopicId, UserId) at Phase 08.
/// NOT audited per spec §4.11.</summary>
public sealed class TopicFollow : Entity<System.Guid>
{
    private TopicFollow(System.Guid id, System.Guid topicId, System.Guid userId,
        System.DateTimeOffset followedOn) : base(id)
    {
        TopicId = topicId; UserId = userId; FollowedOn = followedOn;
    }

    public System.Guid TopicId { get; private set; }
    public System.Guid UserId { get; private set; }
    public System.DateTimeOffset FollowedOn { get; private set; }

    public static TopicFollow Follow(System.Guid topicId, System.Guid userId, ISystemClock clock)
    {
        if (topicId == System.Guid.Empty) throw new DomainException("TopicId is required.");
        if (userId == System.Guid.Empty) throw new DomainException("UserId is required.");
        return new TopicFollow(System.Guid.NewGuid(), topicId, userId, clock.UtcNow);
    }
}
```

- [ ] **Step 2: Tests**

```csharp
using CCE.Domain.Common;
using CCE.Domain.Community;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Community;

public class TopicFollowTests
{
    [Fact]
    public void Follow_creates_association()
    {
        var clock = new FakeSystemClock();
        var f = TopicFollow.Follow(System.Guid.NewGuid(), System.Guid.NewGuid(), clock);
        f.FollowedOn.Should().Be(clock.UtcNow);
    }

    [Fact]
    public void Follow_with_empty_topicId_throws()
    {
        var clock = new FakeSystemClock();
        var act = () => TopicFollow.Follow(System.Guid.Empty, System.Guid.NewGuid(), clock);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Follow_with_empty_userId_throws()
    {
        var clock = new FakeSystemClock();
        var act = () => TopicFollow.Follow(System.Guid.NewGuid(), System.Guid.Empty, clock);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void TopicFollow_is_NOT_audited()
    {
        var attrs = typeof(TopicFollow).GetCustomAttributes(typeof(AuditedAttribute), inherit: false);
        attrs.Should().BeEmpty();
    }
}
```

- [ ] **Step 3: Run + commit**

```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~TopicFollowTests" --logger "console;verbosity=minimal" 2>&1 | tail -3
git add backend/src/CCE.Domain/Community/TopicFollow.cs backend/tests/CCE.Domain.Tests/Community/TopicFollowTests.cs
git -c commit.gpgsign=false commit -m "feat(community): TopicFollow association (4 TDD tests)"
```

---

## Task 5.6: `UserFollow` association (no self-follow)

**Files:**
- Create: `backend/src/CCE.Domain/Community/UserFollow.cs`
- Create: `backend/tests/CCE.Domain.Tests/Community/UserFollowTests.cs`

- [ ] **Step 1: Entity**

```csharp
using CCE.Domain.Common;

namespace CCE.Domain.Community;

/// <summary>User-follows-user. <c>FollowerId ≠ FollowedId</c> invariant.</summary>
public sealed class UserFollow : Entity<System.Guid>
{
    private UserFollow(System.Guid id, System.Guid followerId, System.Guid followedId,
        System.DateTimeOffset followedOn) : base(id)
    {
        FollowerId = followerId; FollowedId = followedId; FollowedOn = followedOn;
    }

    public System.Guid FollowerId { get; private set; }
    public System.Guid FollowedId { get; private set; }
    public System.DateTimeOffset FollowedOn { get; private set; }

    public static UserFollow Follow(System.Guid followerId, System.Guid followedId, ISystemClock clock)
    {
        if (followerId == System.Guid.Empty) throw new DomainException("FollowerId is required.");
        if (followedId == System.Guid.Empty) throw new DomainException("FollowedId is required.");
        if (followerId == followedId)
        {
            throw new DomainException("Users cannot follow themselves.");
        }
        return new UserFollow(System.Guid.NewGuid(), followerId, followedId, clock.UtcNow);
    }
}
```

- [ ] **Step 2: Tests**

```csharp
using CCE.Domain.Common;
using CCE.Domain.Community;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Community;

public class UserFollowTests
{
    [Fact]
    public void Follow_creates_association()
    {
        var clock = new FakeSystemClock();
        var f = UserFollow.Follow(System.Guid.NewGuid(), System.Guid.NewGuid(), clock);
        f.FollowedOn.Should().Be(clock.UtcNow);
    }

    [Fact]
    public void Self_follow_throws()
    {
        var clock = new FakeSystemClock();
        var same = System.Guid.NewGuid();
        var act = () => UserFollow.Follow(same, same, clock);
        act.Should().Throw<DomainException>().WithMessage("*themselves*");
    }

    [Fact]
    public void Follow_with_empty_followerId_throws()
    {
        var clock = new FakeSystemClock();
        var act = () => UserFollow.Follow(System.Guid.Empty, System.Guid.NewGuid(), clock);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Follow_with_empty_followedId_throws()
    {
        var clock = new FakeSystemClock();
        var act = () => UserFollow.Follow(System.Guid.NewGuid(), System.Guid.Empty, clock);
        act.Should().Throw<DomainException>();
    }
}
```

- [ ] **Step 3: Run + commit**

```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~UserFollowTests" --logger "console;verbosity=minimal" 2>&1 | tail -3
git add backend/src/CCE.Domain/Community/UserFollow.cs backend/tests/CCE.Domain.Tests/Community/UserFollowTests.cs
git -c commit.gpgsign=false commit -m "feat(community): UserFollow with no-self-follow invariant (4 TDD tests)"
```

---

## Task 5.7: `PostFollow` association

**Files:**
- Create: `backend/src/CCE.Domain/Community/PostFollow.cs`
- Create: `backend/tests/CCE.Domain.Tests/Community/PostFollowTests.cs`

- [ ] **Step 1: Entity**

```csharp
using CCE.Domain.Common;

namespace CCE.Domain.Community;

public sealed class PostFollow : Entity<System.Guid>
{
    private PostFollow(System.Guid id, System.Guid postId, System.Guid userId,
        System.DateTimeOffset followedOn) : base(id)
    {
        PostId = postId; UserId = userId; FollowedOn = followedOn;
    }

    public System.Guid PostId { get; private set; }
    public System.Guid UserId { get; private set; }
    public System.DateTimeOffset FollowedOn { get; private set; }

    public static PostFollow Follow(System.Guid postId, System.Guid userId, ISystemClock clock)
    {
        if (postId == System.Guid.Empty) throw new DomainException("PostId is required.");
        if (userId == System.Guid.Empty) throw new DomainException("UserId is required.");
        return new PostFollow(System.Guid.NewGuid(), postId, userId, clock.UtcNow);
    }
}
```

- [ ] **Step 2: Tests**

```csharp
using CCE.Domain.Common;
using CCE.Domain.Community;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Community;

public class PostFollowTests
{
    [Fact]
    public void Follow_creates_association()
    {
        var clock = new FakeSystemClock();
        var f = PostFollow.Follow(System.Guid.NewGuid(), System.Guid.NewGuid(), clock);
        f.FollowedOn.Should().Be(clock.UtcNow);
    }

    [Fact]
    public void Empty_postId_throws()
    {
        var clock = new FakeSystemClock();
        var act = () => PostFollow.Follow(System.Guid.Empty, System.Guid.NewGuid(), clock);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Empty_userId_throws()
    {
        var clock = new FakeSystemClock();
        var act = () => PostFollow.Follow(System.Guid.NewGuid(), System.Guid.Empty, clock);
        act.Should().Throw<DomainException>();
    }
}
```

- [ ] **Step 3: Run + commit**

```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~PostFollowTests" --logger "console;verbosity=minimal" 2>&1 | tail -3
git add backend/src/CCE.Domain/Community/PostFollow.cs backend/tests/CCE.Domain.Tests/Community/PostFollowTests.cs
git -c commit.gpgsign=false commit -m "feat(community): PostFollow association (3 TDD tests)"
```

---

## Task 5.8: Audit policy coverage test

**Files:**
- Create: `backend/tests/CCE.Domain.Tests/Community/AuditPolicyTests.cs`

```csharp
using CCE.Domain.Common;
using CCE.Domain.Community;

namespace CCE.Domain.Tests.Community;

public class AuditPolicyTests
{
    [Theory]
    [InlineData(typeof(Topic))]
    [InlineData(typeof(Post))]
    [InlineData(typeof(PostReply))]
    public void Audited_entity_carries_attribute(System.Type type)
    {
        var attrs = type.GetCustomAttributes(typeof(AuditedAttribute), inherit: false);
        attrs.Should().HaveCount(1, because: $"{type.Name} is audited per spec §4.11");
    }

    [Theory]
    [InlineData(typeof(PostRating))]
    [InlineData(typeof(TopicFollow))]
    [InlineData(typeof(UserFollow))]
    [InlineData(typeof(PostFollow))]
    public void Non_audited_high_volume_association_lacks_attribute(System.Type type)
    {
        var attrs = type.GetCustomAttributes(typeof(AuditedAttribute), inherit: false);
        attrs.Should().BeEmpty(because: $"{type.Name} is intentionally NOT audited (high volume — spec §4.11)");
    }
}
```

- [ ] **Step 1: Run + commit**

```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~Community.AuditPolicyTests" --logger "console;verbosity=minimal" 2>&1 | tail -3
git add backend/tests/CCE.Domain.Tests/Community/AuditPolicyTests.cs
git -c commit.gpgsign=false commit -m "test(community): audit policy coverage (3 audited + 4 non-audited = 7 tests)"
```

---

## Task 5.9: Cross-entity integration test (Post + Reply linkage)

**Files:**
- Create: `backend/tests/CCE.Domain.Tests/Community/PostReplyLinkageTests.cs`

```csharp
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
```

- [ ] **Step 1: Run + commit**

```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~PostReplyLinkageTests" --logger "console;verbosity=minimal" 2>&1 | tail -3
git add backend/tests/CCE.Domain.Tests/Community/PostReplyLinkageTests.cs
git -c commit.gpgsign=false commit -m "test(community): Post-Reply linkage + threading (2 integration tests)"
```

---

## Task 5.10: Phase 05 close

- [ ] **Step 1: Full backend run**

```bash
dotnet test backend/CCE.sln --nologo --no-restore --logger "console;verbosity=minimal" 2>&1 | grep -E "(Passed!|Failed!)"
```

- [ ] **Step 2: Update progress doc**

Mark Phase 05 ✅ Done. Use the actual numbers reported.

- [ ] **Step 3: Commit**

```bash
git add docs/subprojects/02-data-domain-progress.md
git -c commit.gpgsign=false commit -m "docs(sub-2): mark Phase 05 done; Community bounded context shipped"
```

---

## Phase 05 — completion checklist

- [ ] 7 Community entities exist under `backend/src/CCE.Domain/Community/`.
- [ ] `Topic` hierarchical + bilingual.
- [ ] `Post` 8000-char limit, locale invariant, answerable flag, MarkAnswered.
- [ ] `PostReply` threaded, IsByExpert, 8000-char limit.
- [ ] `PostRating` 1-5 stars + non-audited.
- [ ] `TopicFollow`, `UserFollow` (no self-follow), `PostFollow` associations.
- [ ] Audit policy correctly applied (3 audited + 4 non-audited).
- [ ] All Phase 04 regression tests still pass.
- [ ] 10 new commits.

**If all boxes ticked, Phase 05 is complete. Proceed to Phase 06 (Knowledge Maps + Interactive City + Notifications + Surveys — 12 entities).**
