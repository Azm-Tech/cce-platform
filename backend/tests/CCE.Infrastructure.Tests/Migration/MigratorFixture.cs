using System.Diagnostics.CodeAnalysis;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using Xunit;

namespace CCE.Infrastructure.Tests.Migration;

/// <summary>
/// xUnit fixture that boots one SQL Server 2022 container shared across
/// all tests in <see cref="MigratorCollection"/>. Each test gets a fresh
/// database name to keep migrations isolated.
/// </summary>
public sealed class MigratorFixture : IAsyncLifetime
{
    public MsSqlContainer Container { get; } = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public async Task InitializeAsync() => await Container.StartAsync().ConfigureAwait(false);
    public async Task DisposeAsync()    => await Container.DisposeAsync().ConfigureAwait(false);

    /// <summary>
    /// Builds a CceDbContext pointing at a freshly-named database on the
    /// shared container. Caller is responsible for disposing.
    /// </summary>
    public CceDbContext CreateContextWithFreshDb(string dbSuffix)
    {
        var conn = BuildConnectionString(dbSuffix);
        var options = new DbContextOptionsBuilder<CceDbContext>()
            .UseSqlServer(conn)
            .UseSnakeCaseNamingConvention()
            .Options;
        return new CceDbContext(options);
    }

    /// <summary>
    /// Returns the connection string for a freshly-named database on the
    /// shared container. Useful when callers need to wire DI with a
    /// connection string (rather than a CceDbContext directly).
    /// </summary>
    public string BuildConnectionString(string dbSuffix)
    {
        var baseConn = Container.GetConnectionString();
        // Force a unique Initial Catalog per call so MigrateAsync gets a clean slate.
        return $"{baseConn};Initial Catalog=CCE_{dbSuffix};TrustServerCertificate=True";
    }
}

[CollectionDefinition(nameof(MigratorCollection))]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix",
    Justification = "xUnit's CollectionDefinition pattern uses 'Collection' as the conventional suffix.")]
public sealed class MigratorCollection : ICollectionFixture<MigratorFixture> { }
