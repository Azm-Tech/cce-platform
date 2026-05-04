using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using CCE.Infrastructure.Tests.Migration;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CCE.Infrastructure.Tests.Backup;

/// <summary>
/// Round-trip tests for the Restore-FromBackup.ps1 workflow. Doesn't
/// invoke PowerShell directly — issues the same RESTORE DATABASE / LOG
/// commands the script issues, against a Testcontainers SQL Server.
/// </summary>
[Collection(nameof(MigratorCollection))]
[SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
    Justification = "Test code; SqlConnections used in tight scope.")]
[SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities",
    Justification = "Test code; SQL composed from test fixture state, not user input.")]
public sealed class RestoreFromBackupTests
{
    private readonly MigratorFixture _fixture;

    public RestoreFromBackupTests(MigratorFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task FullBackup_RestoresToTargetDb_ProducesSameRowCount()
    {
        // Arrange — create a source DB + a few rows + back it up.
        // MigratorFixture prepends "CCE_" to suffixes; SQL uses the full name.
        var srcSuffix = $"src_full_{Guid.NewGuid():N}";
        var tgtSuffix = $"tgt_full_{Guid.NewGuid():N}";
        var sourceDb  = $"CCE_{srcSuffix}";
        var targetDb  = $"CCE_{tgtSuffix}";
        var bakPath   = $"/tmp/{sourceDb}_full.bak";
        var srcCs     = _fixture.BuildConnectionString(srcSuffix);

        await using (var ctx = _fixture.CreateContextWithFreshDb(srcSuffix))
        {
            await ctx.Database.EnsureDeletedAsync();
            await ctx.Database.MigrateAsync();
        }

        // Insert one row that we can verify post-restore.
        await ExecuteAsync(srcCs, $"USE [{sourceDb}]; CREATE TABLE testkv(k nvarchar(50) PRIMARY KEY, v nvarchar(50))");
        await ExecuteAsync(srcCs, $"USE [{sourceDb}]; INSERT INTO testkv VALUES ('hello','world')");

        // Backup.
        await ExecuteAsync(srcCs,
            $"BACKUP DATABASE [{sourceDb}] TO DISK = N'{bakPath}' WITH FORMAT, INIT, COMPRESSION");

        // Act — restore to a different DB name.
        var masterCs = _fixture.Container.GetConnectionString();
        await ExecuteAsync(masterCs,
            $"RESTORE DATABASE [{targetDb}] FROM DISK = N'{bakPath}' " +
            $"WITH FILE = 1, RECOVERY, REPLACE, " +
            $"MOVE N'{sourceDb}' TO N'/var/opt/mssql/data/{targetDb}.mdf', " +
            $"MOVE N'{sourceDb}_log' TO N'/var/opt/mssql/data/{targetDb}_log.ldf'");

        // Assert — restored DB has the row.
        var tgtCs = _fixture.BuildConnectionString(tgtSuffix);
        var count = await ScalarAsync<int>(tgtCs, "SELECT COUNT(*) FROM testkv WHERE k = 'hello' AND v = 'world'");
        count.Should().Be(1);
    }

    [Fact]
    public async Task FullPlusDiffPlusLog_RestoresChain_IncludesAllChanges()
    {
        // MigratorFixture prepends "CCE_" to suffixes; SQL uses the full name.
        var srcSuffix = $"src_chain_{Guid.NewGuid():N}";
        var tgtSuffix = $"tgt_chain_{Guid.NewGuid():N}";
        var sourceDb  = $"CCE_{srcSuffix}";
        var targetDb  = $"CCE_{tgtSuffix}";
        var fullPath  = $"/tmp/{sourceDb}_full.bak";
        var diffPath  = $"/tmp/{sourceDb}_diff.bak";
        var logPath   = $"/tmp/{sourceDb}_log.trn";
        var srcCs     = _fixture.BuildConnectionString(srcSuffix);

        // Migrate the source DB.
        await using (var ctx = _fixture.CreateContextWithFreshDb(srcSuffix))
        {
            await ctx.Database.EnsureDeletedAsync();
            await ctx.Database.MigrateAsync();
        }
        // Force FULL recovery model so log backups work.
        var masterCs = _fixture.Container.GetConnectionString();
        await ExecuteAsync(masterCs, $"ALTER DATABASE [{sourceDb}] SET RECOVERY FULL");

        // Round 1: insert + FULL backup.
        await ExecuteAsync(srcCs, $"USE [{sourceDb}]; CREATE TABLE chain_kv(k nvarchar(50) PRIMARY KEY, v nvarchar(50))");
        await ExecuteAsync(srcCs, $"USE [{sourceDb}]; INSERT INTO chain_kv VALUES ('full', '1')");
        await ExecuteAsync(srcCs,
            $"BACKUP DATABASE [{sourceDb}] TO DISK = N'{fullPath}' WITH FORMAT, INIT, COMPRESSION");

        // Round 2: insert + DIFF backup.
        await ExecuteAsync(srcCs, $"USE [{sourceDb}]; INSERT INTO chain_kv VALUES ('diff', '2')");
        await ExecuteAsync(srcCs,
            $"BACKUP DATABASE [{sourceDb}] TO DISK = N'{diffPath}' WITH DIFFERENTIAL, FORMAT, INIT, COMPRESSION");

        // Round 3: insert + LOG backup.
        await ExecuteAsync(srcCs, $"USE [{sourceDb}]; INSERT INTO chain_kv VALUES ('log', '3')");
        await ExecuteAsync(srcCs,
            $"BACKUP LOG [{sourceDb}] TO DISK = N'{logPath}' WITH FORMAT, INIT, COMPRESSION");

        // Restore the chain.
        await ExecuteAsync(masterCs,
            $"RESTORE DATABASE [{targetDb}] FROM DISK = N'{fullPath}' WITH FILE = 1, NORECOVERY, REPLACE, " +
            $"MOVE N'{sourceDb}' TO N'/var/opt/mssql/data/{targetDb}.mdf', " +
            $"MOVE N'{sourceDb}_log' TO N'/var/opt/mssql/data/{targetDb}_log.ldf'");
        await ExecuteAsync(masterCs,
            $"RESTORE DATABASE [{targetDb}] FROM DISK = N'{diffPath}' WITH FILE = 1, NORECOVERY");
        await ExecuteAsync(masterCs,
            $"RESTORE LOG [{targetDb}] FROM DISK = N'{logPath}' WITH FILE = 1, RECOVERY");

        // Assert — restored DB has all 3 rows.
        var tgtCs = _fixture.BuildConnectionString(tgtSuffix);
        var rows = await ScalarAsync<int>(tgtCs, "SELECT COUNT(*) FROM chain_kv");
        rows.Should().Be(3, "FULL + DIFF + LOG should replay all 3 inserts");

        var hasLogRow = await ScalarAsync<int>(tgtCs, "SELECT COUNT(*) FROM chain_kv WHERE k = 'log'");
        hasLogRow.Should().Be(1, "the log-backup row must be present after the chain restore");
    }

    private static async Task ExecuteAsync(string connectionString, string sql)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.CommandTimeout = 120;
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<T> ScalarAsync<T>(string connectionString, string sql)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.CommandTimeout = 30;
        var result = await cmd.ExecuteScalarAsync();
        return (T)Convert.ChangeType(result!, typeof(T), CultureInfo.InvariantCulture);
    }
}
