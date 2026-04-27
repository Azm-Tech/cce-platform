using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Content;

public class RowVersionContractTests
{
    [Theory]
    [InlineData(typeof(Resource))]
    [InlineData(typeof(News))]
    [InlineData(typeof(Event))]
    [InlineData(typeof(Page))]
    public void Aggregate_root_exposes_byte_array_RowVersion(System.Type type)
    {
        var prop = type.GetProperty("RowVersion",
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic);

        prop.Should().NotBeNull(because: $"{type.Name} should expose a RowVersion property");
        prop!.PropertyType.Should().Be(typeof(byte[]),
            because: $"{type.Name}.RowVersion must be byte[] for SQL Server rowversion mapping");
    }

    [Fact]
    public void Resource_RowVersion_initialised_to_empty_array()
    {
        var clock = new FakeSystemClock();
        var r = Resource.Draft("ا", "x", "ا", "x", ResourceType.Pdf,
            System.Guid.NewGuid(), null, System.Guid.NewGuid(), System.Guid.NewGuid(), clock);
        r.RowVersion.Should().NotBeNull();
        r.RowVersion.Should().BeEmpty();
    }
}
