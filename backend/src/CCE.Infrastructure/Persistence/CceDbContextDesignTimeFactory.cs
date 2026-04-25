using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CCE.Infrastructure.Persistence;

/// <summary>
/// EF Core design-time factory used by <c>dotnet ef migrations add/update</c>.
/// Reads the connection string from the <c>CCE_DESIGN_SQL_CONN</c> env var with a
/// reasonable localhost default for the dev container. Production migrations are
/// applied from the API's runtime composition, never this factory.
/// </summary>
public sealed class CceDbContextDesignTimeFactory : IDesignTimeDbContextFactory<CceDbContext>
{
    public CceDbContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable("CCE_DESIGN_SQL_CONN")
                   ?? "Server=localhost,1433;Database=CCE;User Id=sa;Password=Strong!Passw0rd;TrustServerCertificate=true;";
        var options = new DbContextOptionsBuilder<CceDbContext>()
            .UseSqlServer(conn)
            .UseSnakeCaseNamingConvention()
            .Options;
        return new CceDbContext(options);
    }
}
