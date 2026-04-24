using CCE.Domain.Common;

namespace CCE.Domain.Tests.Common;

public class EntityTests
{
    [Fact]
    public void Two_entities_with_same_id_are_equal()
    {
        var a = new TestEntity(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var b = new TestEntity(Guid.Parse("11111111-1111-1111-1111-111111111111"));

        a.Should().Be(b);
        (a == b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Two_entities_with_different_ids_are_not_equal()
    {
        var a = new TestEntity(Guid.NewGuid());
        var b = new TestEntity(Guid.NewGuid());

        a.Should().NotBe(b);
        (a != b).Should().BeTrue();
    }

    private sealed class TestEntity : Entity<Guid>
    {
        public TestEntity(Guid id) : base(id) { }
    }
}
