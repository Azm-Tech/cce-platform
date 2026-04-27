using CCE.Domain.Common;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Tests.Persistence;

public class DbExceptionMapperTests
{
    [Fact]
    public void DbUpdateConcurrencyException_maps_to_ConcurrencyException()
    {
        var ex = new DbUpdateConcurrencyException("test");
        var mapped = DbExceptionMapper.Map(ex);
        mapped.Should().BeOfType<ConcurrencyException>();
        mapped.InnerException.Should().BeSameAs(ex);
    }

    [Fact]
    public void Unknown_exception_passes_through()
    {
        var ex = new System.InvalidOperationException("not a db error");
        var mapped = DbExceptionMapper.Map(ex);
        mapped.Should().BeSameAs(ex);
    }
}
