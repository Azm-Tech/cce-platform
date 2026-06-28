# Review — Entity Navigation & EF Core Relationships

> Format: each item is a **Bug** (what's wrong + where) followed by a **Fix** (what to do).
> Severity legend: 🟠 likely real issue · 🟡 confirm-then-decide.
>
> **Important framing:** Most "missing navigation property" hits in this solution are *intentional DDD* — aggregate roots referencing each other by **ID only** (no cross-aggregate navigations). Those are correct and listed under "Not a bug." The items below are the ones genuinely worth attention.

---

## 1. 🟡 Within-aggregate FK columns with no relationship configured

**Bug**
Some FK columns have **neither** a navigation property **nor** a `HasOne/HasMany` configuration. With no relationship at all, EF treats them as **plain scalar columns — no FK constraint, no referential integrity** in the DB. For *cross-aggregate* refs this is a valid DDD choice, but these two are **within a single aggregate** and likely should be DB-enforced:

- `Poll.PostId` — `PollConfiguration.cs` only declares a unique index, no relationship to `Post`.
- `Topic.ParentId` (self-referential hierarchy) — `TopicConfiguration.cs` has no parent/child relationship.

**Fix**
1. Confirm against the latest migration whether a FK constraint actually exists for these columns.
2. If not, add an explicit relationship in the configuration (FK only, navigation optional to preserve encapsulation), e.g.:
```csharp
// PollConfiguration
builder.HasOne<Post>().WithOne().HasForeignKey<Poll>(p => p.PostId)
       .OnDelete(DeleteBehavior.Cascade);

// TopicConfiguration (self-ref)
builder.HasOne<Topic>().WithMany().HasForeignKey(t => t.ParentId)
       .OnDelete(DeleteBehavior.Restrict);
```

---

## 2. 🟡 `Post.AnsweredReplyId` not enforced

**Bug**
`Post.AnsweredReplyId` (the reply marked as the accepted answer) has no relationship configured in `PostConfiguration.cs`. Nothing guarantees it points to a real `PostReply` of that post; it's a loose scalar.

**Fix**
Add an explicit optional FK relationship to `PostReply` (no navigation needed):
```csharp
builder.HasOne<PostReply>().WithMany().HasForeignKey(p => p.AnsweredReplyId)
       .OnDelete(DeleteBehavior.NoAction);
```
Confirm the chosen delete behavior doesn't create a multiple-cascade-path conflict with the existing Post→Replies cascade.

---

## 3. 🟠 Implicit cascade delete on `HomepageSettings.Countries`

**Bug**
`HomepageSettingsConfiguration.cs:22` configures `HasMany(s => s.Countries).WithOne().HasForeignKey(c => c.HomepageSettingsId)` with **no explicit `OnDelete`**. EF defaults to cascade here, but every comparable relationship in the solution states it explicitly — this one is the odd one out, which violates the project's "make cascade explicit" convention.

**Fix**
Add the explicit behavior:
```csharp
builder.HasMany(s => s.Countries)
       .WithOne()
       .HasForeignKey(c => c.HomepageSettingsId)
       .OnDelete(DeleteBehavior.Cascade);
```

---

## Not a bug (verified — intentional DDD, leave as-is)

- **Cross-aggregate references by ID with no navigation:** `CityScenario.UserId`, `KnowledgeMapNode.MapId`, `KnowledgeMapEdge.*`, and the `UserFollow` / `PostFollow` / `TopicFollow` join entities. Referencing other aggregate roots by ID (not navigation) is the correct DDD pattern; adding navigations would weaken aggregate boundaries.
- **Tag many-to-many one-way design:** `News`/`Event`/`Post` expose `IReadOnlyCollection<Tag>` via `.UsingEntity(...)`, while `Tag` has no back-collection. Tag is a lookup; a back-reference to every content type is unnecessary and undesirable. Intentional.
- **`PollOption` / `ResourceCountry` with no back-navigation to their parent:** correct — these are owned/child entities of their aggregate; the parent owns the collection and children don't need a back-reference.
- **Encapsulated collections** (`IReadOnlyCollection` exposed over a private `List` backing field) are used consistently — no public collection setters bypassing encapsulation were found.
- **Owned value objects** (`HomepageSettings.Objective`, `PolicySection.Title/Content` via `OwnsOne` with `LocalizedText`) are configured correctly.
- **Soft-delete filtered unique indexes** (Topic, Community, KnowledgeMap, Country, ExpertProfile) are configured correctly.
