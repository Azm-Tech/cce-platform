using CCE.Infrastructure.Identity;

namespace CCE.Infrastructure.Tests.Identity;

public class SystemCountryScopeAccessorTests
{
    [Fact]
    public async Task Returns_null()
    {
        var sut = new SystemCountryScopeAccessor();
        var result = await sut.GetAuthorizedCountryIdsAsync(CancellationToken.None);
        result.Should().BeNull();
    }
}
