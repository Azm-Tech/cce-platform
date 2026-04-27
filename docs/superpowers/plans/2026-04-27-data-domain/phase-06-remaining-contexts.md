# Phase 06 — Knowledge Maps + Interactive City + Notifications + Surveys

> Parent: [`../2026-04-27-data-domain.md`](../2026-04-27-data-domain.md) · Spec: [`../../specs/2026-04-27-data-domain-design.md`](../../specs/2026-04-27-data-domain-design.md) §4.5–§4.8

**Phase goal:** Land 12 entities across 4 bounded contexts: `KnowledgeMaps/` (4 entities), `InteractiveCity/` (3), `Notifications/` (2), `Surveys/` (2; co-located in `Surveys/` folder). Pure domain layer.

**Tasks in this phase:** 12
**Working directory:** `/Users/m/CCE/`
**Preconditions:** Phase 05 closed (`b9676a2` is HEAD); 281 backend tests passing.

---

## Task 6.1: KnowledgeMaps enums

**Files:** `backend/src/CCE.Domain/KnowledgeMaps/{NodeType,RelationshipType,AssociatedType}.cs`

```csharp
// NodeType.cs
namespace CCE.Domain.KnowledgeMaps;
public enum NodeType { Technology = 0, Sector = 1, SubTopic = 2 }

// RelationshipType.cs
namespace CCE.Domain.KnowledgeMaps;
public enum RelationshipType { ParentOf = 0, RelatedTo = 1, RequiredBy = 2 }

// AssociatedType.cs
namespace CCE.Domain.KnowledgeMaps;
public enum AssociatedType { Resource = 0, News = 1, Event = 2 }
```

- [ ] Build + commit:
```bash
dotnet build backend/src/CCE.Domain/CCE.Domain.csproj --nologo --no-restore 2>&1 | tail -3
git add backend/src/CCE.Domain/KnowledgeMaps/
git -c commit.gpgsign=false commit -m "feat(km): 3 KnowledgeMaps enums (NodeType, RelationshipType, AssociatedType)"
```

---

## Task 6.2: `KnowledgeMap` aggregate

**Files:** `backend/src/CCE.Domain/KnowledgeMaps/KnowledgeMap.cs`, `backend/tests/CCE.Domain.Tests/KnowledgeMaps/KnowledgeMapTests.cs`

- [ ] **Step 1: Entity**

```csharp
using System.Text.RegularExpressions;
using CCE.Domain.Common;

namespace CCE.Domain.KnowledgeMaps;

[Audited]
public sealed class KnowledgeMap : AggregateRoot<System.Guid>, ISoftDeletable
{
    private static readonly Regex SlugPattern = new("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);

    private KnowledgeMap(System.Guid id, string nameAr, string nameEn,
        string descriptionAr, string descriptionEn, string slug) : base(id)
    {
        NameAr = nameAr; NameEn = nameEn;
        DescriptionAr = descriptionAr; DescriptionEn = descriptionEn;
        Slug = slug; IsActive = true;
    }

    public string NameAr { get; private set; }
    public string NameEn { get; private set; }
    public string DescriptionAr { get; private set; }
    public string DescriptionEn { get; private set; }
    public string Slug { get; private set; }
    public bool IsActive { get; private set; }
    public byte[] RowVersion { get; private set; } = System.Array.Empty<byte>();
    public bool IsDeleted { get; private set; }
    public System.DateTimeOffset? DeletedOn { get; private set; }
    public System.Guid? DeletedById { get; private set; }

    public static KnowledgeMap Create(string nameAr, string nameEn,
        string descriptionAr, string descriptionEn, string slug)
    {
        if (string.IsNullOrWhiteSpace(nameAr)) throw new DomainException("NameAr is required.");
        if (string.IsNullOrWhiteSpace(nameEn)) throw new DomainException("NameEn is required.");
        if (string.IsNullOrWhiteSpace(descriptionAr)) throw new DomainException("DescriptionAr is required.");
        if (string.IsNullOrWhiteSpace(descriptionEn)) throw new DomainException("DescriptionEn is required.");
        if (string.IsNullOrWhiteSpace(slug) || !SlugPattern.IsMatch(slug))
            throw new DomainException($"slug '{slug}' must be kebab-case.");
        return new KnowledgeMap(System.Guid.NewGuid(), nameAr, nameEn, descriptionAr, descriptionEn, slug);
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

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

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
using CCE.Domain.KnowledgeMaps;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.KnowledgeMaps;

public class KnowledgeMapTests
{
    private static KnowledgeMap NewMap() => KnowledgeMap.Create(
        "خريطة", "Map", "وصف", "Description", "carbon-cycle");

    [Fact]
    public void Create_active_map() {
        var m = NewMap();
        m.IsActive.Should().BeTrue();
        m.Slug.Should().Be("carbon-cycle");
        m.RowVersion.Should().NotBeNull();
    }

    [Fact]
    public void Slug_must_be_kebab_case() {
        var act = () => KnowledgeMap.Create("ا", "x", "ا", "x", "Bad Slug");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void UpdateContent_replaces_bilingual_fields() {
        var m = NewMap();
        m.UpdateContent("ج", "new", "ج", "new");
        m.NameEn.Should().Be("new");
    }

    [Fact]
    public void Deactivate_then_Activate_toggles() {
        var m = NewMap();
        m.Deactivate(); m.IsActive.Should().BeFalse();
        m.Activate(); m.IsActive.Should().BeTrue();
    }

    [Fact]
    public void SoftDelete_marks_deleted() {
        var m = NewMap();
        m.SoftDelete(System.Guid.NewGuid(), new FakeSystemClock());
        m.IsDeleted.Should().BeTrue();
    }
}
```

- [ ] **Step 3: Run + commit:**
```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~KnowledgeMapTests" --logger "console;verbosity=minimal" 2>&1 | tail -3
git add backend/src/CCE.Domain/KnowledgeMaps/KnowledgeMap.cs backend/tests/CCE.Domain.Tests/KnowledgeMaps/KnowledgeMapTests.cs
git -c commit.gpgsign=false commit -m "feat(km): KnowledgeMap aggregate (5 TDD tests)"
```

---

## Task 6.3: `KnowledgeMapNode`

**Files:** `backend/src/CCE.Domain/KnowledgeMaps/KnowledgeMapNode.cs`, `backend/tests/CCE.Domain.Tests/KnowledgeMaps/KnowledgeMapNodeTests.cs`

- [ ] **Step 1: Entity**

```csharp
using CCE.Domain.Common;

namespace CCE.Domain.KnowledgeMaps;

public sealed class KnowledgeMapNode : Entity<System.Guid>
{
    private KnowledgeMapNode(System.Guid id, System.Guid mapId, string nameAr, string nameEn,
        NodeType nodeType, string? descriptionAr, string? descriptionEn,
        string? iconUrl, double layoutX, double layoutY, int orderIndex) : base(id)
    {
        MapId = mapId; NameAr = nameAr; NameEn = nameEn;
        NodeType = nodeType; DescriptionAr = descriptionAr; DescriptionEn = descriptionEn;
        IconUrl = iconUrl; LayoutX = layoutX; LayoutY = layoutY; OrderIndex = orderIndex;
    }

    public System.Guid MapId { get; private set; }
    public string NameAr { get; private set; }
    public string NameEn { get; private set; }
    public NodeType NodeType { get; private set; }
    public string? DescriptionAr { get; private set; }
    public string? DescriptionEn { get; private set; }
    public string? IconUrl { get; private set; }
    public double LayoutX { get; private set; }
    public double LayoutY { get; private set; }
    public int OrderIndex { get; private set; }

    public static KnowledgeMapNode Create(System.Guid mapId, string nameAr, string nameEn,
        NodeType nodeType, string? descriptionAr, string? descriptionEn,
        string? iconUrl, double layoutX, double layoutY, int orderIndex)
    {
        if (mapId == System.Guid.Empty) throw new DomainException("MapId is required.");
        if (string.IsNullOrWhiteSpace(nameAr)) throw new DomainException("NameAr is required.");
        if (string.IsNullOrWhiteSpace(nameEn)) throw new DomainException("NameEn is required.");
        if (iconUrl is not null
            && !iconUrl.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase))
            throw new DomainException("IconUrl must be https://.");
        return new KnowledgeMapNode(System.Guid.NewGuid(), mapId, nameAr, nameEn,
            nodeType, descriptionAr, descriptionEn, iconUrl, layoutX, layoutY, orderIndex);
    }

    public void UpdateLayout(double x, double y) { LayoutX = x; LayoutY = y; }
    public void Reorder(int orderIndex) => OrderIndex = orderIndex;
}
```

- [ ] **Step 2: Tests**

```csharp
using CCE.Domain.Common;
using CCE.Domain.KnowledgeMaps;

namespace CCE.Domain.Tests.KnowledgeMaps;

public class KnowledgeMapNodeTests
{
    private static KnowledgeMapNode NewNode() => KnowledgeMapNode.Create(
        System.Guid.NewGuid(), "تقنية", "Tech", NodeType.Technology, null, null,
        null, 100, 200, 0);

    [Fact]
    public void Create_node() { NewNode().NodeType.Should().Be(NodeType.Technology); }

    [Fact]
    public void Empty_mapId_throws() {
        var act = () => KnowledgeMapNode.Create(System.Guid.Empty, "ا", "x",
            NodeType.Sector, null, null, null, 0, 0, 0);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void IconUrl_must_be_https() {
        var act = () => KnowledgeMapNode.Create(System.Guid.NewGuid(), "ا", "x",
            NodeType.Sector, null, null, "http://insecure", 0, 0, 0);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void UpdateLayout_changes_coordinates() {
        var n = NewNode();
        n.UpdateLayout(500, 600);
        n.LayoutX.Should().Be(500);
        n.LayoutY.Should().Be(600);
    }

    [Fact]
    public void Reorder_updates_OrderIndex() {
        var n = NewNode();
        n.Reorder(7);
        n.OrderIndex.Should().Be(7);
    }
}
```

- [ ] **Step 3: Run + commit:**
```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~KnowledgeMapNodeTests" --logger "console;verbosity=minimal" 2>&1 | tail -3
git add backend/src/CCE.Domain/KnowledgeMaps/KnowledgeMapNode.cs backend/tests/CCE.Domain.Tests/KnowledgeMaps/KnowledgeMapNodeTests.cs
git -c commit.gpgsign=false commit -m "feat(km): KnowledgeMapNode entity (5 TDD tests)"
```

---

## Task 6.4: `KnowledgeMapEdge` (no self-loops)

**Files:** `backend/src/CCE.Domain/KnowledgeMaps/KnowledgeMapEdge.cs`, `backend/tests/CCE.Domain.Tests/KnowledgeMaps/KnowledgeMapEdgeTests.cs`

- [ ] **Step 1: Entity**

```csharp
using CCE.Domain.Common;

namespace CCE.Domain.KnowledgeMaps;

/// <summary>Edge between two nodes. <c>FromNodeId ≠ ToNodeId</c> invariant.</summary>
public sealed class KnowledgeMapEdge : Entity<System.Guid>
{
    private KnowledgeMapEdge(System.Guid id, System.Guid mapId, System.Guid fromNodeId,
        System.Guid toNodeId, RelationshipType relationshipType, int orderIndex) : base(id)
    {
        MapId = mapId; FromNodeId = fromNodeId; ToNodeId = toNodeId;
        RelationshipType = relationshipType; OrderIndex = orderIndex;
    }

    public System.Guid MapId { get; private set; }
    public System.Guid FromNodeId { get; private set; }
    public System.Guid ToNodeId { get; private set; }
    public RelationshipType RelationshipType { get; private set; }
    public int OrderIndex { get; private set; }

    public static KnowledgeMapEdge Connect(System.Guid mapId, System.Guid fromNodeId,
        System.Guid toNodeId, RelationshipType relationshipType, int orderIndex = 0)
    {
        if (mapId == System.Guid.Empty) throw new DomainException("MapId is required.");
        if (fromNodeId == System.Guid.Empty) throw new DomainException("FromNodeId is required.");
        if (toNodeId == System.Guid.Empty) throw new DomainException("ToNodeId is required.");
        if (fromNodeId == toNodeId) throw new DomainException("Self-loop not allowed (FromNodeId == ToNodeId).");
        return new KnowledgeMapEdge(System.Guid.NewGuid(), mapId, fromNodeId, toNodeId, relationshipType, orderIndex);
    }
}
```

- [ ] **Step 2: Tests**

```csharp
using CCE.Domain.Common;
using CCE.Domain.KnowledgeMaps;

namespace CCE.Domain.Tests.KnowledgeMaps;

public class KnowledgeMapEdgeTests
{
    [Fact]
    public void Connect_creates_edge() {
        var e = KnowledgeMapEdge.Connect(
            System.Guid.NewGuid(), System.Guid.NewGuid(), System.Guid.NewGuid(),
            RelationshipType.RelatedTo);
        e.RelationshipType.Should().Be(RelationshipType.RelatedTo);
    }

    [Fact]
    public void Self_loop_throws() {
        var node = System.Guid.NewGuid();
        var act = () => KnowledgeMapEdge.Connect(System.Guid.NewGuid(), node, node, RelationshipType.ParentOf);
        act.Should().Throw<DomainException>().WithMessage("*Self-loop*");
    }

    [Fact]
    public void Empty_mapId_throws() {
        var act = () => KnowledgeMapEdge.Connect(System.Guid.Empty,
            System.Guid.NewGuid(), System.Guid.NewGuid(), RelationshipType.ParentOf);
        act.Should().Throw<DomainException>();
    }
}
```

- [ ] **Step 3: Run + commit:**
```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~KnowledgeMapEdgeTests" --logger "console;verbosity=minimal" 2>&1 | tail -3
git add backend/src/CCE.Domain/KnowledgeMaps/KnowledgeMapEdge.cs backend/tests/CCE.Domain.Tests/KnowledgeMaps/KnowledgeMapEdgeTests.cs
git -c commit.gpgsign=false commit -m "feat(km): KnowledgeMapEdge with no-self-loop invariant (3 TDD tests)"
```

---

## Task 6.5: `KnowledgeMapAssociation`

**Files:** `backend/src/CCE.Domain/KnowledgeMaps/KnowledgeMapAssociation.cs`, `backend/tests/CCE.Domain.Tests/KnowledgeMaps/KnowledgeMapAssociationTests.cs`

- [ ] **Step 1: Entity**

```csharp
using CCE.Domain.Common;

namespace CCE.Domain.KnowledgeMaps;

/// <summary>
/// Polymorphic association from a node to one of (Resource, News, Event). The pair
/// (<see cref="AssociatedType"/>, <see cref="AssociatedId"/>) is the FK; FK enforcement is
/// application-side (Phase 08 doesn't add a real FK because the target table varies).
/// </summary>
public sealed class KnowledgeMapAssociation : Entity<System.Guid>
{
    private KnowledgeMapAssociation(System.Guid id, System.Guid nodeId,
        AssociatedType associatedType, System.Guid associatedId, int orderIndex) : base(id)
    {
        NodeId = nodeId; AssociatedType = associatedType;
        AssociatedId = associatedId; OrderIndex = orderIndex;
    }

    public System.Guid NodeId { get; private set; }
    public AssociatedType AssociatedType { get; private set; }
    public System.Guid AssociatedId { get; private set; }
    public int OrderIndex { get; private set; }

    public static KnowledgeMapAssociation Associate(System.Guid nodeId,
        AssociatedType associatedType, System.Guid associatedId, int orderIndex = 0)
    {
        if (nodeId == System.Guid.Empty) throw new DomainException("NodeId is required.");
        if (associatedId == System.Guid.Empty) throw new DomainException("AssociatedId is required.");
        return new KnowledgeMapAssociation(System.Guid.NewGuid(), nodeId, associatedType, associatedId, orderIndex);
    }
}
```

- [ ] **Step 2: Tests**

```csharp
using CCE.Domain.Common;
using CCE.Domain.KnowledgeMaps;

namespace CCE.Domain.Tests.KnowledgeMaps;

public class KnowledgeMapAssociationTests
{
    [Fact]
    public void Associate_to_resource() {
        var a = KnowledgeMapAssociation.Associate(
            System.Guid.NewGuid(), AssociatedType.Resource, System.Guid.NewGuid());
        a.AssociatedType.Should().Be(AssociatedType.Resource);
    }

    [Fact]
    public void Empty_nodeId_throws() {
        var act = () => KnowledgeMapAssociation.Associate(
            System.Guid.Empty, AssociatedType.Event, System.Guid.NewGuid());
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Empty_associatedId_throws() {
        var act = () => KnowledgeMapAssociation.Associate(
            System.Guid.NewGuid(), AssociatedType.News, System.Guid.Empty);
        act.Should().Throw<DomainException>();
    }
}
```

- [ ] **Step 3: Run + commit:**
```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~KnowledgeMapAssociationTests" --logger "console;verbosity=minimal" 2>&1 | tail -3
git add backend/src/CCE.Domain/KnowledgeMaps/KnowledgeMapAssociation.cs backend/tests/CCE.Domain.Tests/KnowledgeMaps/KnowledgeMapAssociationTests.cs
git -c commit.gpgsign=false commit -m "feat(km): KnowledgeMapAssociation polymorphic association (3 TDD tests)"
```

---

## Task 6.6: `CityType` enum + `CityTechnology` entity

**Files:** `backend/src/CCE.Domain/InteractiveCity/{CityType,CityTechnology}.cs`, `backend/tests/CCE.Domain.Tests/InteractiveCity/CityTechnologyTests.cs`

- [ ] **Step 1: Enum + entity**

```csharp
// CityType.cs
namespace CCE.Domain.InteractiveCity;
public enum CityType { Coastal = 0, Industrial = 1, Mixed = 2, Residential = 3 }
```

```csharp
// CityTechnology.cs
using CCE.Domain.Common;

namespace CCE.Domain.InteractiveCity;

public sealed class CityTechnology : Entity<System.Guid>
{
    private CityTechnology(System.Guid id, string nameAr, string nameEn,
        string descriptionAr, string descriptionEn, string categoryAr, string categoryEn,
        decimal carbonImpactKgPerYear, decimal costUsd, string? iconUrl) : base(id)
    {
        NameAr = nameAr; NameEn = nameEn;
        DescriptionAr = descriptionAr; DescriptionEn = descriptionEn;
        CategoryAr = categoryAr; CategoryEn = categoryEn;
        CarbonImpactKgPerYear = carbonImpactKgPerYear; CostUsd = costUsd;
        IconUrl = iconUrl; IsActive = true;
    }

    public string NameAr { get; private set; }
    public string NameEn { get; private set; }
    public string DescriptionAr { get; private set; }
    public string DescriptionEn { get; private set; }
    public string CategoryAr { get; private set; }
    public string CategoryEn { get; private set; }
    public decimal CarbonImpactKgPerYear { get; private set; }
    public decimal CostUsd { get; private set; }
    public string? IconUrl { get; private set; }
    public bool IsActive { get; private set; }

    public static CityTechnology Create(string nameAr, string nameEn,
        string descriptionAr, string descriptionEn, string categoryAr, string categoryEn,
        decimal carbonImpactKgPerYear, decimal costUsd, string? iconUrl = null)
    {
        if (string.IsNullOrWhiteSpace(nameAr)) throw new DomainException("NameAr is required.");
        if (string.IsNullOrWhiteSpace(nameEn)) throw new DomainException("NameEn is required.");
        if (string.IsNullOrWhiteSpace(descriptionAr)) throw new DomainException("DescriptionAr is required.");
        if (string.IsNullOrWhiteSpace(descriptionEn)) throw new DomainException("DescriptionEn is required.");
        if (string.IsNullOrWhiteSpace(categoryAr)) throw new DomainException("CategoryAr is required.");
        if (string.IsNullOrWhiteSpace(categoryEn)) throw new DomainException("CategoryEn is required.");
        if (costUsd < 0) throw new DomainException("CostUsd cannot be negative.");
        if (iconUrl is not null
            && !iconUrl.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase))
            throw new DomainException("IconUrl must be https://.");
        return new CityTechnology(System.Guid.NewGuid(), nameAr, nameEn,
            descriptionAr, descriptionEn, categoryAr, categoryEn,
            carbonImpactKgPerYear, costUsd, iconUrl);
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    public void UpdateImpact(decimal carbonImpactKgPerYear, decimal costUsd)
    {
        if (costUsd < 0) throw new DomainException("CostUsd cannot be negative.");
        CarbonImpactKgPerYear = carbonImpactKgPerYear;
        CostUsd = costUsd;
    }
}
```

- [ ] **Step 2: Tests**

```csharp
using CCE.Domain.Common;
using CCE.Domain.InteractiveCity;

namespace CCE.Domain.Tests.InteractiveCity;

public class CityTechnologyTests
{
    private static CityTechnology NewTech() => CityTechnology.Create(
        "ألواح", "Solar Panels", "وصف", "Description", "الطاقة", "Energy",
        carbonImpactKgPerYear: -2500m, costUsd: 15000m);

    [Fact]
    public void Create_active_with_negative_carbon_impact_for_reductions() {
        var t = NewTech();
        t.IsActive.Should().BeTrue();
        t.CarbonImpactKgPerYear.Should().Be(-2500m);
    }

    [Fact]
    public void Negative_cost_throws() {
        var act = () => CityTechnology.Create(
            "ا", "x", "ا", "x", "ا", "x", -100m, -1m);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Empty_required_field_throws() {
        var act = () => CityTechnology.Create(
            "", "x", "ا", "x", "ا", "x", 0m, 0m);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void UpdateImpact_changes_values() {
        var t = NewTech();
        t.UpdateImpact(-3000m, 18000m);
        t.CarbonImpactKgPerYear.Should().Be(-3000m);
        t.CostUsd.Should().Be(18000m);
    }

    [Fact]
    public void Deactivate_then_Activate_toggles() {
        var t = NewTech();
        t.Deactivate();
        t.IsActive.Should().BeFalse();
        t.Activate();
        t.IsActive.Should().BeTrue();
    }
}
```

- [ ] **Step 3: Run + commit:**
```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~CityTechnologyTests" --logger "console;verbosity=minimal" 2>&1 | tail -3
git add backend/src/CCE.Domain/InteractiveCity/CityType.cs backend/src/CCE.Domain/InteractiveCity/CityTechnology.cs backend/tests/CCE.Domain.Tests/InteractiveCity/CityTechnologyTests.cs
git -c commit.gpgsign=false commit -m "feat(city): CityType enum + CityTechnology entity (5 TDD tests)"
```

---

## Task 6.7: `CityScenario` aggregate

**Files:** `backend/src/CCE.Domain/InteractiveCity/CityScenario.cs`, `backend/tests/CCE.Domain.Tests/InteractiveCity/CityScenarioTests.cs`

- [ ] **Step 1: Entity**

```csharp
using CCE.Domain.Common;

namespace CCE.Domain.InteractiveCity;

[Audited]
public sealed class CityScenario : AggregateRoot<System.Guid>, ISoftDeletable
{
    public const int MinTargetYear = 2030;
    public const int MaxTargetYear = 2080;

    private CityScenario(System.Guid id, System.Guid userId, string nameAr, string nameEn,
        CityType cityType, int targetYear, string configurationJson,
        System.DateTimeOffset createdOn) : base(id)
    {
        UserId = userId; NameAr = nameAr; NameEn = nameEn;
        CityType = cityType; TargetYear = targetYear;
        ConfigurationJson = configurationJson;
        CreatedOn = createdOn; LastModifiedOn = createdOn;
    }

    public System.Guid UserId { get; private set; }
    public string NameAr { get; private set; }
    public string NameEn { get; private set; }
    public CityType CityType { get; private set; }
    public int TargetYear { get; private set; }
    public string ConfigurationJson { get; private set; }
    public System.DateTimeOffset CreatedOn { get; private set; }
    public System.DateTimeOffset LastModifiedOn { get; private set; }
    public bool IsDeleted { get; private set; }
    public System.DateTimeOffset? DeletedOn { get; private set; }
    public System.Guid? DeletedById { get; private set; }

    public static CityScenario Create(System.Guid userId, string nameAr, string nameEn,
        CityType cityType, int targetYear, string configurationJson, ISystemClock clock)
    {
        if (userId == System.Guid.Empty) throw new DomainException("UserId is required.");
        if (string.IsNullOrWhiteSpace(nameAr)) throw new DomainException("NameAr is required.");
        if (string.IsNullOrWhiteSpace(nameEn)) throw new DomainException("NameEn is required.");
        if (targetYear < MinTargetYear || targetYear > MaxTargetYear)
            throw new DomainException($"TargetYear must be between {MinTargetYear} and {MaxTargetYear}.");
        if (string.IsNullOrWhiteSpace(configurationJson))
            throw new DomainException("ConfigurationJson is required.");
        return new CityScenario(System.Guid.NewGuid(), userId, nameAr, nameEn,
            cityType, targetYear, configurationJson, clock.UtcNow);
    }

    public void UpdateConfiguration(string configurationJson, ISystemClock clock)
    {
        if (string.IsNullOrWhiteSpace(configurationJson))
            throw new DomainException("ConfigurationJson is required.");
        ConfigurationJson = configurationJson;
        LastModifiedOn = clock.UtcNow;
    }

    public void Rename(string nameAr, string nameEn, ISystemClock clock)
    {
        if (string.IsNullOrWhiteSpace(nameAr)) throw new DomainException("NameAr is required.");
        if (string.IsNullOrWhiteSpace(nameEn)) throw new DomainException("NameEn is required.");
        NameAr = nameAr; NameEn = nameEn;
        LastModifiedOn = clock.UtcNow;
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
using CCE.Domain.InteractiveCity;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.InteractiveCity;

public class CityScenarioTests
{
    private static CityScenario NewScenario(FakeSystemClock clock) => CityScenario.Create(
        System.Guid.NewGuid(), "خطتي", "My Plan", CityType.Mixed,
        2050, "{\"techs\": []}", clock);

    [Fact]
    public void Create_scenario() {
        var s = NewScenario(new FakeSystemClock());
        s.CityType.Should().Be(CityType.Mixed);
        s.TargetYear.Should().Be(2050);
        s.IsDeleted.Should().BeFalse();
    }

    [Theory]
    [InlineData(2025)]
    [InlineData(2090)]
    public void TargetYear_outside_range_throws(int badYear) {
        var clock = new FakeSystemClock();
        var act = () => CityScenario.Create(System.Guid.NewGuid(), "ا", "x",
            CityType.Coastal, badYear, "{}", clock);
        act.Should().Throw<DomainException>().WithMessage("*TargetYear*");
    }

    [Fact]
    public void UpdateConfiguration_advances_LastModifiedOn() {
        var clock = new FakeSystemClock();
        var s = NewScenario(clock);
        clock.Advance(System.TimeSpan.FromHours(2));
        s.UpdateConfiguration("{\"techs\": [\"solar\"]}", clock);
        s.LastModifiedOn.Should().Be(clock.UtcNow);
        s.ConfigurationJson.Should().Contain("solar");
    }

    [Fact]
    public void Rename_updates_names_and_modified() {
        var clock = new FakeSystemClock();
        var s = NewScenario(clock);
        clock.Advance(System.TimeSpan.FromHours(1));
        s.Rename("جديد", "New", clock);
        s.NameEn.Should().Be("New");
        s.LastModifiedOn.Should().Be(clock.UtcNow);
    }

    [Fact]
    public void SoftDelete_marks_deleted() {
        var clock = new FakeSystemClock();
        var s = NewScenario(clock);
        s.SoftDelete(System.Guid.NewGuid(), clock);
        s.IsDeleted.Should().BeTrue();
    }
}
```

- [ ] **Step 3: Run + commit:**
```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~CityScenarioTests" --logger "console;verbosity=minimal" 2>&1 | tail -3
git add backend/src/CCE.Domain/InteractiveCity/CityScenario.cs backend/tests/CCE.Domain.Tests/InteractiveCity/CityScenarioTests.cs
git -c commit.gpgsign=false commit -m "feat(city): CityScenario aggregate with target-year invariant (6 TDD tests)"
```

---

## Task 6.8: `CityScenarioResult` (append-only)

**Files:** `backend/src/CCE.Domain/InteractiveCity/CityScenarioResult.cs`, `backend/tests/CCE.Domain.Tests/InteractiveCity/CityScenarioResultTests.cs`

- [ ] **Step 1: Entity**

```csharp
using CCE.Domain.Common;

namespace CCE.Domain.InteractiveCity;

/// <summary>
/// Append-only computation result for a <see cref="CityScenario"/>. The simulation engine
/// produces one of these per run; the front-end charts the latest one. NOT audited (high-volume).
/// </summary>
public sealed class CityScenarioResult : Entity<System.Guid>
{
    private CityScenarioResult(System.Guid id, System.Guid scenarioId,
        int? computedCarbonNeutralityYear, decimal computedTotalCostUsd,
        System.DateTimeOffset computedAt, string engineVersion) : base(id)
    {
        ScenarioId = scenarioId;
        ComputedCarbonNeutralityYear = computedCarbonNeutralityYear;
        ComputedTotalCostUsd = computedTotalCostUsd;
        ComputedAt = computedAt;
        EngineVersion = engineVersion;
    }

    public System.Guid ScenarioId { get; private set; }
    public int? ComputedCarbonNeutralityYear { get; private set; }
    public decimal ComputedTotalCostUsd { get; private set; }
    public System.DateTimeOffset ComputedAt { get; private set; }
    public string EngineVersion { get; private set; }

    public static CityScenarioResult Compute(System.Guid scenarioId,
        int? computedCarbonNeutralityYear, decimal computedTotalCostUsd,
        string engineVersion, ISystemClock clock)
    {
        if (scenarioId == System.Guid.Empty) throw new DomainException("ScenarioId is required.");
        if (string.IsNullOrWhiteSpace(engineVersion))
            throw new DomainException("EngineVersion is required.");
        if (computedTotalCostUsd < 0)
            throw new DomainException("ComputedTotalCostUsd cannot be negative.");
        return new CityScenarioResult(System.Guid.NewGuid(), scenarioId,
            computedCarbonNeutralityYear, computedTotalCostUsd, clock.UtcNow, engineVersion);
    }
}
```

- [ ] **Step 2: Tests**

```csharp
using CCE.Domain.Common;
using CCE.Domain.InteractiveCity;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.InteractiveCity;

public class CityScenarioResultTests
{
    [Fact]
    public void Compute_creates_result() {
        var clock = new FakeSystemClock();
        var r = CityScenarioResult.Compute(
            System.Guid.NewGuid(), 2055, 120_000m, "engine-v1.2", clock);
        r.ComputedCarbonNeutralityYear.Should().Be(2055);
        r.EngineVersion.Should().Be("engine-v1.2");
    }

    [Fact]
    public void Compute_with_null_neutrality_year_allowed() {
        var clock = new FakeSystemClock();
        var r = CityScenarioResult.Compute(
            System.Guid.NewGuid(), null, 50_000m, "v1", clock);
        r.ComputedCarbonNeutralityYear.Should().BeNull();
    }

    [Fact]
    public void Negative_cost_throws() {
        var clock = new FakeSystemClock();
        var act = () => CityScenarioResult.Compute(
            System.Guid.NewGuid(), 2050, -1m, "v1", clock);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Empty_engineVersion_throws() {
        var clock = new FakeSystemClock();
        var act = () => CityScenarioResult.Compute(
            System.Guid.NewGuid(), 2050, 100m, "", clock);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Result_is_NOT_audited() {
        typeof(CityScenarioResult).GetCustomAttributes(typeof(AuditedAttribute), inherit: false)
            .Should().BeEmpty();
    }
}
```

- [ ] **Step 3: Run + commit:**
```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~CityScenarioResultTests" --logger "console;verbosity=minimal" 2>&1 | tail -3
git add backend/src/CCE.Domain/InteractiveCity/CityScenarioResult.cs backend/tests/CCE.Domain.Tests/InteractiveCity/CityScenarioResultTests.cs
git -c commit.gpgsign=false commit -m "feat(city): CityScenarioResult append-only (5 TDD tests)"
```

---

## Task 6.9: `NotificationChannel` enum + `NotificationTemplate`

**Files:** `backend/src/CCE.Domain/Notifications/{NotificationChannel,NotificationStatus,NotificationTemplate}.cs`, `backend/tests/CCE.Domain.Tests/Notifications/NotificationTemplateTests.cs`

- [ ] **Step 1: Enums + entity**

```csharp
// NotificationChannel.cs
namespace CCE.Domain.Notifications;
public enum NotificationChannel { Email = 0, Sms = 1, InApp = 2 }
```

```csharp
// NotificationStatus.cs
namespace CCE.Domain.Notifications;
public enum NotificationStatus { Pending = 0, Sent = 1, Failed = 2, Read = 3 }
```

```csharp
// NotificationTemplate.cs
using System.Text.RegularExpressions;
using CCE.Domain.Common;

namespace CCE.Domain.Notifications;

[Audited]
public sealed class NotificationTemplate : Entity<System.Guid>
{
    private static readonly Regex CodePattern = new("^[A-Z][A-Z0-9_]+$", RegexOptions.Compiled);

    private NotificationTemplate(System.Guid id, string code,
        string subjectAr, string subjectEn, string bodyAr, string bodyEn,
        NotificationChannel channel, string variableSchemaJson) : base(id)
    {
        Code = code;
        SubjectAr = subjectAr; SubjectEn = subjectEn;
        BodyAr = bodyAr; BodyEn = bodyEn;
        Channel = channel; VariableSchemaJson = variableSchemaJson;
        IsActive = true;
    }

    public string Code { get; private set; }
    public string SubjectAr { get; private set; }
    public string SubjectEn { get; private set; }
    public string BodyAr { get; private set; }
    public string BodyEn { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public string VariableSchemaJson { get; private set; }
    public bool IsActive { get; private set; }

    public static NotificationTemplate Define(string code,
        string subjectAr, string subjectEn, string bodyAr, string bodyEn,
        NotificationChannel channel, string variableSchemaJson)
    {
        if (string.IsNullOrWhiteSpace(code) || !CodePattern.IsMatch(code))
            throw new DomainException($"Code '{code}' must be UPPER_SNAKE_CASE.");
        if (string.IsNullOrWhiteSpace(subjectAr)) throw new DomainException("SubjectAr is required.");
        if (string.IsNullOrWhiteSpace(subjectEn)) throw new DomainException("SubjectEn is required.");
        if (string.IsNullOrWhiteSpace(bodyAr)) throw new DomainException("BodyAr is required.");
        if (string.IsNullOrWhiteSpace(bodyEn)) throw new DomainException("BodyEn is required.");
        if (string.IsNullOrWhiteSpace(variableSchemaJson))
            throw new DomainException("VariableSchemaJson is required (use '{}' for none).");
        return new NotificationTemplate(System.Guid.NewGuid(), code,
            subjectAr, subjectEn, bodyAr, bodyEn, channel, variableSchemaJson);
    }

    public void UpdateContent(string subjectAr, string subjectEn, string bodyAr, string bodyEn)
    {
        if (string.IsNullOrWhiteSpace(subjectAr)) throw new DomainException("SubjectAr is required.");
        if (string.IsNullOrWhiteSpace(subjectEn)) throw new DomainException("SubjectEn is required.");
        if (string.IsNullOrWhiteSpace(bodyAr)) throw new DomainException("BodyAr is required.");
        if (string.IsNullOrWhiteSpace(bodyEn)) throw new DomainException("BodyEn is required.");
        SubjectAr = subjectAr; SubjectEn = subjectEn;
        BodyAr = bodyAr; BodyEn = bodyEn;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
```

- [ ] **Step 2: Tests**

```csharp
using CCE.Domain.Common;
using CCE.Domain.Notifications;

namespace CCE.Domain.Tests.Notifications;

public class NotificationTemplateTests
{
    private static NotificationTemplate NewTemplate() => NotificationTemplate.Define(
        "ACCOUNT_CREATED", "تم إنشاء حسابك", "Your account is created",
        "مرحباً", "Welcome", NotificationChannel.Email, "{}");

    [Fact]
    public void Define_creates_active_template() {
        var t = NewTemplate();
        t.IsActive.Should().BeTrue();
        t.Channel.Should().Be(NotificationChannel.Email);
    }

    [Theory]
    [InlineData("lowercase")]
    [InlineData("Mixed_Case")]
    [InlineData("HAS-DASH")]
    [InlineData("123_LEADING_DIGIT")]
    public void Code_must_be_upper_snake_case(string bad) {
        var act = () => NotificationTemplate.Define(
            bad, "ا", "x", "ا", "x", NotificationChannel.Email, "{}");
        act.Should().Throw<DomainException>().WithMessage("*Code*");
    }

    [Fact]
    public void UpdateContent_replaces_subject_body() {
        var t = NewTemplate();
        t.UpdateContent("ج", "new subject", "ج", "new body");
        t.SubjectEn.Should().Be("new subject");
        t.BodyAr.Should().Be("ج");
    }

    [Fact]
    public void Deactivate_then_Activate_toggles() {
        var t = NewTemplate();
        t.Deactivate();
        t.IsActive.Should().BeFalse();
        t.Activate();
        t.IsActive.Should().BeTrue();
    }
}
```

- [ ] **Step 3: Run + commit:**
```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~NotificationTemplateTests" --logger "console;verbosity=minimal" 2>&1 | tail -3
git add backend/src/CCE.Domain/Notifications/NotificationChannel.cs backend/src/CCE.Domain/Notifications/NotificationStatus.cs backend/src/CCE.Domain/Notifications/NotificationTemplate.cs backend/tests/CCE.Domain.Tests/Notifications/NotificationTemplateTests.cs
git -c commit.gpgsign=false commit -m "feat(notifications): NotificationTemplate + Channel/Status enums (7 TDD tests)"
```

---

## Task 6.10: `UserNotification` + state machine

**Files:** `backend/src/CCE.Domain/Notifications/UserNotification.cs`, `backend/tests/CCE.Domain.Tests/Notifications/UserNotificationTests.cs`

- [ ] **Step 1: Entity**

```csharp
using CCE.Domain.Common;

namespace CCE.Domain.Notifications;

/// <summary>
/// One rendered notification delivered to a user. State machine:
/// <c>Pending → Sent → (Read | Failed)</c>. NOT audited (high-volume time-series).
/// </summary>
public sealed class UserNotification : Entity<System.Guid>
{
    private UserNotification(System.Guid id, System.Guid userId, System.Guid templateId,
        string renderedSubjectAr, string renderedSubjectEn, string renderedBody,
        string renderedLocale, NotificationChannel channel) : base(id)
    {
        UserId = userId; TemplateId = templateId;
        RenderedSubjectAr = renderedSubjectAr; RenderedSubjectEn = renderedSubjectEn;
        RenderedBody = renderedBody; RenderedLocale = renderedLocale;
        Channel = channel; Status = NotificationStatus.Pending;
    }

    public System.Guid UserId { get; private set; }
    public System.Guid TemplateId { get; private set; }
    public string RenderedSubjectAr { get; private set; }
    public string RenderedSubjectEn { get; private set; }
    public string RenderedBody { get; private set; }
    public string RenderedLocale { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public System.DateTimeOffset? SentOn { get; private set; }
    public System.DateTimeOffset? ReadOn { get; private set; }
    public NotificationStatus Status { get; private set; }

    public static UserNotification Render(System.Guid userId, System.Guid templateId,
        string renderedSubjectAr, string renderedSubjectEn, string renderedBody,
        string renderedLocale, NotificationChannel channel)
    {
        if (userId == System.Guid.Empty) throw new DomainException("UserId is required.");
        if (templateId == System.Guid.Empty) throw new DomainException("TemplateId is required.");
        if (string.IsNullOrWhiteSpace(renderedBody)) throw new DomainException("RenderedBody is required.");
        if (renderedLocale != "ar" && renderedLocale != "en")
            throw new DomainException("RenderedLocale must be 'ar' or 'en'.");
        return new UserNotification(System.Guid.NewGuid(), userId, templateId,
            renderedSubjectAr, renderedSubjectEn, renderedBody, renderedLocale, channel);
    }

    public void MarkSent(ISystemClock clock)
    {
        if (Status != NotificationStatus.Pending)
            throw new DomainException($"Cannot send a {Status} notification — must be Pending.");
        Status = NotificationStatus.Sent;
        SentOn = clock.UtcNow;
    }

    public void MarkFailed(ISystemClock clock)
    {
        if (Status != NotificationStatus.Pending)
            throw new DomainException($"Cannot fail a {Status} notification — must be Pending.");
        Status = NotificationStatus.Failed;
    }

    public void MarkRead(ISystemClock clock)
    {
        if (Status != NotificationStatus.Sent)
            throw new DomainException($"Cannot mark {Status} notification as read — must be Sent.");
        Status = NotificationStatus.Read;
        ReadOn = clock.UtcNow;
    }
}
```

- [ ] **Step 2: Tests**

```csharp
using CCE.Domain.Common;
using CCE.Domain.Notifications;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Notifications;

public class UserNotificationTests
{
    private static UserNotification NewPending() => UserNotification.Render(
        System.Guid.NewGuid(), System.Guid.NewGuid(),
        "موضوع", "Subject", "نص", "ar", NotificationChannel.Email);

    [Fact]
    public void Render_creates_pending() {
        var n = NewPending();
        n.Status.Should().Be(NotificationStatus.Pending);
        n.SentOn.Should().BeNull();
    }

    [Fact]
    public void MarkSent_transitions_from_Pending() {
        var clock = new FakeSystemClock();
        var n = NewPending();
        n.MarkSent(clock);
        n.Status.Should().Be(NotificationStatus.Sent);
        n.SentOn.Should().Be(clock.UtcNow);
    }

    [Fact]
    public void MarkRead_transitions_from_Sent() {
        var clock = new FakeSystemClock();
        var n = NewPending();
        n.MarkSent(clock);
        clock.Advance(System.TimeSpan.FromMinutes(5));
        n.MarkRead(clock);
        n.Status.Should().Be(NotificationStatus.Read);
        n.ReadOn.Should().Be(clock.UtcNow);
    }

    [Fact]
    public void MarkFailed_from_Pending() {
        var clock = new FakeSystemClock();
        var n = NewPending();
        n.MarkFailed(clock);
        n.Status.Should().Be(NotificationStatus.Failed);
    }

    [Fact]
    public void Cannot_send_already_sent() {
        var clock = new FakeSystemClock();
        var n = NewPending();
        n.MarkSent(clock);
        var act = () => n.MarkSent(clock);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Cannot_mark_pending_as_read() {
        var clock = new FakeSystemClock();
        var n = NewPending();
        var act = () => n.MarkRead(clock);
        act.Should().Throw<DomainException>().WithMessage("*Sent*");
    }

    [Fact]
    public void Invalid_locale_throws() {
        var act = () => UserNotification.Render(
            System.Guid.NewGuid(), System.Guid.NewGuid(), "ا", "x", "y", "fr", NotificationChannel.Sms);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void UserNotification_is_NOT_audited() {
        typeof(UserNotification).GetCustomAttributes(typeof(AuditedAttribute), inherit: false)
            .Should().BeEmpty();
    }
}
```

- [ ] **Step 3: Run + commit:**
```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~UserNotificationTests" --logger "console;verbosity=minimal" 2>&1 | tail -3
git add backend/src/CCE.Domain/Notifications/UserNotification.cs backend/tests/CCE.Domain.Tests/Notifications/UserNotificationTests.cs
git -c commit.gpgsign=false commit -m "feat(notifications): UserNotification with Pending→Sent→Read state machine (8 TDD tests)"
```

---

## Task 6.11: Surveys (`ServiceRating` + `SearchQueryLog`)

**Files:** `backend/src/CCE.Domain/Surveys/{ServiceRating,SearchQueryLog}.cs`, `backend/tests/CCE.Domain.Tests/Surveys/{ServiceRatingTests,SearchQueryLogTests}.cs`

- [ ] **Step 1: Entities**

```csharp
// ServiceRating.cs
using CCE.Domain.Common;

namespace CCE.Domain.Surveys;

public sealed class ServiceRating : Entity<System.Guid>
{
    private ServiceRating(System.Guid id, System.Guid? userId, int rating,
        string? commentAr, string? commentEn, string page, string locale,
        System.DateTimeOffset submittedOn) : base(id)
    {
        UserId = userId; Rating = rating;
        CommentAr = commentAr; CommentEn = commentEn;
        Page = page; Locale = locale; SubmittedOn = submittedOn;
    }

    public System.Guid? UserId { get; private set; }
    public int Rating { get; private set; }
    public string? CommentAr { get; private set; }
    public string? CommentEn { get; private set; }
    public string Page { get; private set; }
    public string Locale { get; private set; }
    public System.DateTimeOffset SubmittedOn { get; private set; }

    public static ServiceRating Submit(System.Guid? userId, int rating,
        string? commentAr, string? commentEn, string page, string locale, ISystemClock clock)
    {
        if (rating < 1 || rating > 5)
            throw new DomainException($"Rating must be 1-5 (got {rating}).");
        if (string.IsNullOrWhiteSpace(page)) throw new DomainException("Page is required.");
        if (locale != "ar" && locale != "en")
            throw new DomainException("locale must be 'ar' or 'en'.");
        return new ServiceRating(System.Guid.NewGuid(), userId, rating,
            commentAr, commentEn, page, locale, clock.UtcNow);
    }
}
```

```csharp
// SearchQueryLog.cs
using CCE.Domain.Common;

namespace CCE.Domain.Surveys;

public sealed class SearchQueryLog : Entity<System.Guid>
{
    private SearchQueryLog(System.Guid id, System.Guid? userId, string queryText,
        int resultsCount, int responseTimeMs, string locale,
        System.DateTimeOffset submittedOn) : base(id)
    {
        UserId = userId; QueryText = queryText;
        ResultsCount = resultsCount; ResponseTimeMs = responseTimeMs;
        Locale = locale; SubmittedOn = submittedOn;
    }

    public System.Guid? UserId { get; private set; }
    public string QueryText { get; private set; }
    public int ResultsCount { get; private set; }
    public int ResponseTimeMs { get; private set; }
    public string Locale { get; private set; }
    public System.DateTimeOffset SubmittedOn { get; private set; }

    public static SearchQueryLog Record(System.Guid? userId, string queryText,
        int resultsCount, int responseTimeMs, string locale, ISystemClock clock)
    {
        if (string.IsNullOrWhiteSpace(queryText)) throw new DomainException("QueryText is required.");
        if (resultsCount < 0) throw new DomainException("ResultsCount cannot be negative.");
        if (responseTimeMs < 0) throw new DomainException("ResponseTimeMs cannot be negative.");
        if (locale != "ar" && locale != "en")
            throw new DomainException("locale must be 'ar' or 'en'.");
        return new SearchQueryLog(System.Guid.NewGuid(), userId, queryText,
            resultsCount, responseTimeMs, locale, clock.UtcNow);
    }
}
```

- [ ] **Step 2: Tests**

```csharp
// ServiceRatingTests.cs
using CCE.Domain.Common;
using CCE.Domain.Surveys;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Surveys;

public class ServiceRatingTests
{
    [Fact]
    public void Submit_anonymous_rating() {
        var r = ServiceRating.Submit(null, 4, null, null, "/home", "ar", new FakeSystemClock());
        r.UserId.Should().BeNull();
        r.Rating.Should().Be(4);
    }

    [Fact]
    public void Submit_with_user_and_comment() {
        var clock = new FakeSystemClock();
        var user = System.Guid.NewGuid();
        var r = ServiceRating.Submit(user, 5, "ممتاز", "Excellent", "/about", "en", clock);
        r.UserId.Should().Be(user);
        r.CommentEn.Should().Be("Excellent");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Out_of_range_rating_throws(int bad) {
        var act = () => ServiceRating.Submit(null, bad, null, null, "/x", "ar", new FakeSystemClock());
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Empty_page_throws() {
        var act = () => ServiceRating.Submit(null, 3, null, null, "", "ar", new FakeSystemClock());
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Invalid_locale_throws() {
        var act = () => ServiceRating.Submit(null, 3, null, null, "/x", "fr", new FakeSystemClock());
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ServiceRating_is_NOT_audited() {
        typeof(ServiceRating).GetCustomAttributes(typeof(AuditedAttribute), inherit: false)
            .Should().BeEmpty();
    }
}
```

```csharp
// SearchQueryLogTests.cs
using CCE.Domain.Common;
using CCE.Domain.Surveys;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Surveys;

public class SearchQueryLogTests
{
    [Fact]
    public void Record_search() {
        var log = SearchQueryLog.Record(null, "carbon capture", 47, 120, "en", new FakeSystemClock());
        log.QueryText.Should().Be("carbon capture");
        log.ResultsCount.Should().Be(47);
    }

    [Fact]
    public void Empty_queryText_throws() {
        var act = () => SearchQueryLog.Record(null, "", 0, 100, "ar", new FakeSystemClock());
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Negative_resultsCount_throws() {
        var act = () => SearchQueryLog.Record(null, "x", -1, 100, "ar", new FakeSystemClock());
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Negative_responseTime_throws() {
        var act = () => SearchQueryLog.Record(null, "x", 0, -1, "ar", new FakeSystemClock());
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void SearchQueryLog_is_NOT_audited() {
        typeof(SearchQueryLog).GetCustomAttributes(typeof(AuditedAttribute), inherit: false)
            .Should().BeEmpty();
    }
}
```

- [ ] **Step 3: Run + commit:**
```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~Surveys" --logger "console;verbosity=minimal" 2>&1 | tail -3
git add backend/src/CCE.Domain/Surveys/ backend/tests/CCE.Domain.Tests/Surveys/
git -c commit.gpgsign=false commit -m "feat(surveys): ServiceRating + SearchQueryLog (12 TDD tests)"
```

---

## Task 6.12: Phase 06 close

- [ ] **Step 1: Full backend run**

```bash
dotnet test backend/CCE.sln --nologo --no-restore --logger "console;verbosity=minimal" 2>&1 | grep -E "(Passed!|Failed!)"
```

- [ ] **Step 2: Update progress doc**

Mark Phase 06 ✅ Done. Use the actual numbers reported.

- [ ] **Step 3: Commit**

```bash
git add docs/subprojects/02-data-domain-progress.md
git -c commit.gpgsign=false commit -m "docs(sub-2): mark Phase 06 done; KM + City + Notif + Surveys shipped"
```

---

## Phase 06 — completion checklist

- [ ] 12 entities across `KnowledgeMaps/`, `InteractiveCity/`, `Notifications/`, `Surveys/`.
- [ ] 7 enums (NodeType, RelationshipType, AssociatedType, CityType, NotificationChannel, NotificationStatus, plus existing).
- [ ] No-self-loop on `KnowledgeMapEdge`.
- [ ] Polymorphic FK on `KnowledgeMapAssociation`.
- [ ] Target-year invariant on `CityScenario` (2030–2080).
- [ ] State machine on `UserNotification` (Pending→Sent→Read|Failed).
- [ ] Audit policy: `KnowledgeMap`, `CityScenario`, `NotificationTemplate` audited; the rest not (high-volume per §4.11).
- [ ] All Phase 05 regression tests still pass.
- [ ] 12 new commits.

**If all boxes ticked, Phase 06 is complete. Proceed to Phase 07 (Persistence wiring — CceDbContext, configurations, soft-delete filter, AuditingInterceptor, DomainEventDispatcher, DbExceptionMapper).**
