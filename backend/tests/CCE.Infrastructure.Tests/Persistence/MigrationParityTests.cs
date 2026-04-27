using System.Diagnostics;
using System.Text;

namespace CCE.Infrastructure.Tests.Persistence;

/// <summary>
/// Re-runs <c>dotnet ef migrations script</c> against the current model and asserts
/// the output equals the committed snapshot. Catches model-vs-migration drift.
/// </summary>
public class MigrationParityTests
{
    [Fact(Skip = "Requires dotnet-ef tool on PATH and a built CceDbContext; run locally with `dotnet test --filter MigrationParityTests` after `dotnet build`.")]
    public void Migrations_script_matches_committed_snapshot()
    {
        var repoRoot = FindRepoRoot();
        var snapshotPath = Path.Combine(repoRoot,
            "backend/src/CCE.Infrastructure/Persistence/Migrations/data-domain-initial-script.sql");
        File.Exists(snapshotPath).Should().BeTrue();

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "ef migrations script 0 DataDomainInitial " +
                        "--project src/CCE.Infrastructure/CCE.Infrastructure.csproj " +
                        "--startup-project src/CCE.Infrastructure/CCE.Infrastructure.csproj " +
                        "--context CceDbContext --no-build",
            WorkingDirectory = Path.Combine(repoRoot, "backend"),
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };
        using var process = Process.Start(psi);
        process.Should().NotBeNull();
        var script = process!.StandardOutput.ReadToEnd();
        process.WaitForExit();
        process.ExitCode.Should().Be(0);

        var committed = File.ReadAllText(snapshotPath, Encoding.UTF8);
        Normalize(script).Should().Be(Normalize(committed));
    }

    private static string Normalize(string sql) =>
        sql.Replace("\r\n", "\n", System.StringComparison.Ordinal).TrimEnd();

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "backend/CCE.sln")))
        {
            dir = dir.Parent;
        }
        if (dir is null) throw new System.IO.DirectoryNotFoundException("repo root not found");
        return dir.FullName;
    }
}
