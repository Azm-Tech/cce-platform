using CCE.Domain.Audit;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Tests.Persistence;

public class CceDbContextTests
{
    private static string ConnectionString =>
        $"Server=localhost,1433;Database=CCE;User Id=sa;Password={GetPassword()};TrustServerCertificate=true;";

    private static string GetPassword()
    {
        var envFile = Path.Combine(GetRepoRoot(), ".env");
        if (!File.Exists(envFile))
        {
            return "Strong!Passw0rd";
        }
        foreach (var line in File.ReadAllLines(envFile))
        {
            if (line.StartsWith("SQL_PASSWORD=", StringComparison.Ordinal))
            {
                return line["SQL_PASSWORD=".Length..];
            }
        }
        return "Strong!Passw0rd";
    }

    private static string GetRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, ".env.example")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? throw new InvalidOperationException("Could not locate repo root.");
    }

    private static CceDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<CceDbContext>()
            .UseSqlServer(ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;
        return new CceDbContext(options);
    }

    [Fact]
    public async Task Round_trips_AuditEvent_through_SQL()
    {
        await using var ctx = NewContext();

        var id = Guid.NewGuid();
        var occurredOn = new DateTimeOffset(2026, 4, 25, 14, 0, 0, TimeSpan.Zero);
        var correlationId = Guid.NewGuid();
        var entity = new AuditEvent(
            id,
            occurredOn,
            actor: "test-user@cce.local",
            action: "Test.Insert",
            resource: $"AuditEvent/{id}",
            correlationId: correlationId,
            diff: "{\"smoke\":true}");

        ctx.AuditEvents.Add(entity);
        await ctx.SaveChangesAsync();

        // Re-fetch in a new context to confirm the row hit SQL (not just change-tracker)
        await using var ctx2 = NewContext();
        var found = await ctx2.AuditEvents.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);

        found.Should().NotBeNull();
        found!.Actor.Should().Be("test-user@cce.local");
        found.Action.Should().Be("Test.Insert");
        found.CorrelationId.Should().Be(correlationId);
        found.Diff.Should().Be("{\"smoke\":true}");
        found.OccurredOn.Should().Be(occurredOn);

        // Cleanup so re-runs are deterministic
        ctx2.AuditEvents.Remove(found);
        await ctx2.SaveChangesAsync();
    }

    [Fact]
    public async Task Schema_has_expected_indexes()
    {
        await using var ctx = NewContext();

        var indexes = await ctx.Database.SqlQuery<string>(
            $"SELECT name FROM sys.indexes WHERE object_id = OBJECT_ID('audit_events') AND name IS NOT NULL ORDER BY name")
            .ToListAsync();

        indexes.Should().Contain("ix_audit_events_actor_occurred_on");
        indexes.Should().Contain("ix_audit_events_correlation_id");
    }
}
