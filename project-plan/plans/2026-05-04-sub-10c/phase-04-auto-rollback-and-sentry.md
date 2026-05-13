# Phase 04 — Auto-rollback + Sentry production DSN (Sub-10c)

> Parent: [`../2026-05-04-sub-10c.md`](../2026-05-04-sub-10c.md) · Spec: [`../../specs/2026-05-04-sub-10c-design.md`](../../specs/2026-05-04-sub-10c-design.md) §Auto-rollback, §Sentry production DSN.

**Phase goal:** Wire `-AutoRollback` flow into `deploy.ps1` Step 8 (with the `-Recursive` recursion guard already added in Phase 00), make `LoggingExtensions.UseCceSerilog` propagate `SENTRY_ENVIRONMENT` + `SENTRY_RELEASE` tags into Sentry events, send a Sentry breadcrumb on auto-rollback. Tests + Sentry alert-rules runbook.

**Tasks:** 4
**Working directory:** `/Users/m/CCE/`
**Preconditions:**
- Phase 03 closed (5 commits land on `main`; HEAD at `2269204` or later).
- `.env.<env>.example` has `AUTO_ROLLBACK`, `SENTRY_ENVIRONMENT`, `SENTRY_RELEASE` (all from Phase 00 Task 0.3).
- `deploy.ps1` already has `-Recursive` switch as a no-op stub from Phase 00 Task 0.1.
- `rollback.ps1` already passes `-Recursive` to nested `deploy.ps1` from Phase 00 Task 0.2.
- Backend baseline: 439 Application + 71 Infrastructure tests passing (1 skipped).

---

## Task 4.1: `deploy.ps1` `-AutoRollback` + Step 8 auto-rollback flow

**Files:**
- Modify: `deploy/deploy.ps1` — add `-AutoRollback` + `-NoAutoRollback` switches; modify Step 8 so smoke-probe failure triggers auto-rollback when enabled (env-file `AUTO_ROLLBACK=true` OR `-AutoRollback` flag, unless `-NoAutoRollback` overrides). Use the existing `-Recursive` switch (set by `rollback.ps1`) to suppress recursion.

**Resolution rules** (precedence high to low):
1. `-NoAutoRollback` flag → never auto-rollback (operator's manual override).
2. `-AutoRollback` flag → always auto-rollback when smoke fails.
3. `-Recursive` flag set → never auto-rollback (we're already inside a rollback's nested deploy; would loop).
4. Env-file `AUTO_ROLLBACK=true` → auto-rollback.
5. Default → don't auto-rollback (Sub-10b behaviour).

**Rollback target resolution:**
- Read most recent `OK` row in `deploy-history-${env}.tsv` whose tag differs from current `CCE_IMAGE_TAG`.
- If no prior `OK` row exists, log "no prior good tag — auto-rollback skipped" and exit non-zero (operator-driven rollback still possible).

**Final state of the new param block:**

```powershell
[CmdletBinding()]
param(
    [ValidateSet('test','preprod','prod','dr')]
    [string]$Environment = 'prod',
    [string]$EnvFile,
    [switch]$Recursive,
    [switch]$AutoRollback,
    [switch]$NoAutoRollback
)
```

**Final state of the modified Step 8 + new auto-rollback helper:**

Replace the existing Step 8 block (single `if` line) with:

```powershell
# ─── Step 8: Smoke probe ──────────────────────────────────────────────────
Write-Log "Step 8/10: Running smoke probes."
$smokeScript = Join-Path $PSScriptRoot 'smoke.ps1'
& pwsh -NoProfile -File $smokeScript -Timeout 60
$smokeExitCode = $LASTEXITCODE

if ($smokeExitCode -ne 0) {
    # Resolve auto-rollback decision.
    $autoRollbackEnabled = $false
    if ($NoAutoRollback) {
        Write-Log "Smoke failed; -NoAutoRollback override — leaving apps running for operator inspection."
    } elseif ($Recursive) {
        Write-Log "Smoke failed; -Recursive set (nested deploy from rollback.ps1) — recursion guard suppresses auto-rollback."
    } elseif ($AutoRollback) {
        Write-Log "Smoke failed; -AutoRollback flag — attempting auto-rollback."
        $autoRollbackEnabled = $true
    } elseif ($envMap['AUTO_ROLLBACK'] -ieq 'true') {
        Write-Log "Smoke failed; AUTO_ROLLBACK=true in env-file — attempting auto-rollback."
        $autoRollbackEnabled = $true
    } else {
        Write-Log "Smoke failed; auto-rollback NOT enabled."
    }

    if ($autoRollbackEnabled) {
        # Resolve previous OK tag from deploy-history-${env}.tsv.
        $currentTag = $envMap['CCE_IMAGE_TAG']
        $previousTag = $null
        if (Test-Path $historyFile) {
            $okRows = Get-Content $historyFile | Where-Object { $_ -match '\tOK(\t|$)' }
            # TSV columns: <UTC-iso8601> \t <sha> \t <tag> \t OK [\t ROLLBACK_FROM=...]
            # Walk newest → oldest, pick first with a tag != current.
            for ($i = $okRows.Count - 1; $i -ge 0; $i--) {
                $cols = $okRows[$i].Split("`t")
                if ($cols.Count -ge 3 -and $cols[2] -and $cols[2] -ne $currentTag) {
                    $previousTag = $cols[2]
                    break
                }
            }
        }

        if (-not $previousTag) {
            Abort "Auto-rollback enabled but no prior OK tag found in $historyFile. Operator-driven rollback only."
        }

        Write-Log "Auto-rolling back to '$previousTag' (current was '$currentTag')."

        # Send Sentry breadcrumb (best-effort; no-op if SENTRY_DSN not set).
        Send-SentryBreadcrumb -EnvMap $envMap -CurrentTag $currentTag -PreviousTag $previousTag -Reason "smoke-probe failure"

        # Invoke rollback.ps1, which atomically rewrites .env.<env> and re-runs deploy.ps1 -Recursive.
        $rollbackScript = Join-Path $PSScriptRoot 'rollback.ps1'
        & pwsh -NoProfile -File $rollbackScript -ToTag $previousTag -Environment $Environment -EnvFile $resolvedEnvFile
        $rollbackExitCode = $LASTEXITCODE

        if ($rollbackExitCode -ne 0) {
            Abort "Auto-rollback FAILED (rollback.ps1 exit $rollbackExitCode). Manual intervention required. Both bad tag '$currentTag' and rollback target '$previousTag' may be unhealthy."
        }
        Write-Log "Auto-rollback complete; live tag is now '$previousTag'."
        exit 0   # Auto-rollback succeeded — operator's deploy "failed" (the new tag) but the system is healthy.
    }

    Abort "Smoke probe failed. Apps left running for inspection." -ShowRollback
}
```

**`Send-SentryBreadcrumb` helper** (added near `Write-Log` / `Abort`):

```powershell
function Send-SentryBreadcrumb {
    param(
        [hashtable]$EnvMap,
        [string]$CurrentTag,
        [string]$PreviousTag,
        [string]$Reason
    )
    $dsn = $EnvMap['SENTRY_DSN']
    if ([string]::IsNullOrWhiteSpace($dsn)) {
        Write-Log "SENTRY_DSN not set; skipping auto-rollback Sentry event."
        return
    }
    # Sentry DSN format: https://<key>@<host>/<project_id>
    $match = [regex]::Match($dsn, '^https://([^@]+)@([^/]+)/(.+)$')
    if (-not $match.Success) {
        Write-Log -Level 'WARN' "SENTRY_DSN looks malformed; skipping breadcrumb."
        return
    }
    $key = $match.Groups[1].Value
    $host = $match.Groups[2].Value
    $projectId = $match.Groups[3].Value

    $payload = @{
        message = "deploy.auto_rollback: $CurrentTag → $PreviousTag ($Reason)"
        level = 'error'
        environment = $EnvMap['SENTRY_ENVIRONMENT'] ?? $Environment
        release = $CurrentTag
        tags = @{
            'deploy.auto_rollback' = 'true'
            'deploy.from_tag' = $CurrentTag
            'deploy.to_tag' = $PreviousTag
        }
        extra = @{
            reason = $Reason
            cce_environment = $Environment
            host = $env:COMPUTERNAME
        }
    } | ConvertTo-Json -Depth 10

    $sentryUrl = "https://$host/api/$projectId/store/"
    $sentryAuth = "Sentry sentry_version=7,sentry_key=$key,sentry_client=cce-deploy/1.0"
    try {
        Invoke-RestMethod -Uri $sentryUrl -Method Post `
            -Headers @{ 'X-Sentry-Auth' = $sentryAuth; 'Content-Type' = 'application/json' } `
            -Body $payload -TimeoutSec 5 | Out-Null
        Write-Log "Sentry auto-rollback event sent."
    } catch {
        Write-Log -Level 'WARN' "Sentry breadcrumb POST failed (non-fatal): $($_.Exception.Message)"
    }
}
```

**Operator UX** (to ensure the runbook accurately documents what happens):

| Trigger | Behaviour |
|---|---|
| `.\deploy\deploy.ps1 -Environment test` (env has `AUTO_ROLLBACK=true`), smoke fails | Auto-rollback to previous tag; deploy.ps1 exits 0 (system healthy) |
| `.\deploy\deploy.ps1 -Environment prod` (env has `AUTO_ROLLBACK=false`), smoke fails | Apps left running; deploy.ps1 exits non-zero with rollback hint |
| `.\deploy\deploy.ps1 -Environment prod -AutoRollback`, smoke fails | Auto-rollback even though env says false |
| `.\deploy\deploy.ps1 -Environment test -NoAutoRollback`, smoke fails | Apps left running even though env says true |
| `rollback.ps1 -ToTag <prev>` → calls deploy.ps1 -Recursive, smoke fails | No further auto-rollback (recursion guard); deploy.ps1 fails non-zero with manual-intervention message |

- [ ] **Step 1:** Read `deploy/deploy.ps1` to understand the existing param block + Step 8.
  ```bash
  grep -n "param(\|Step 8\|Smoke probe\|Abort\|envMap\|historyFile" /Users/m/CCE/deploy/deploy.ps1
  ```

- [ ] **Step 2:** Apply the param block change (add `-AutoRollback` + `-NoAutoRollback`).

- [ ] **Step 3:** Add the `Send-SentryBreadcrumb` helper function near `Abort`.

- [ ] **Step 4:** Replace Step 8's single `if ($LASTEXITCODE -ne 0) { Abort ... -ShowRollback }` with the auto-rollback flow above.

- [ ] **Step 5:** Verify build / parse:
  ```bash
  pwsh -NoProfile -Command "& { \$null = [System.Management.Automation.Language.Parser]::ParseFile('/Users/m/CCE/deploy/deploy.ps1', [ref]\$null, [ref]\$err); if (\$err) { \$err | Out-Host; exit 1 } else { Write-Host 'parses OK' } }"
  ```
  Skip if no pwsh; deploy-smoke.yml will catch syntax issues.

- [ ] **Step 6:** Commit:
  ```bash
  git -C /Users/m/CCE add deploy/deploy.ps1
  git -C /Users/m/CCE -c commit.gpgsign=false commit -m "feat(deploy): -AutoRollback flow in Step 8 + Sentry breadcrumb

  deploy.ps1 gains -AutoRollback + -NoAutoRollback switches.
  When smoke probe fails: -NoAutoRollback wins; -Recursive
  (nested call from rollback.ps1) suppresses; -AutoRollback flag
  forces; env-file AUTO_ROLLBACK=true triggers; default leaves
  apps running per Sub-10b behaviour.

  On trigger: resolves previous OK tag from deploy-history-<env>.tsv,
  sends best-effort Sentry breadcrumb event (deploy.auto_rollback
  tag), invokes rollback.ps1. Recursion guard via -Recursive
  prevents loops. Both-fail mode logs manual-intervention message.

  Sub-10c Phase 04 Task 4.1.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Task 4.2: `LoggingExtensions.UseCceSerilog` reads `SENTRY_ENVIRONMENT` + `SENTRY_RELEASE`

**Files:**
- Modify: `backend/src/CCE.Api.Common/Observability/LoggingExtensions.cs` — add `o.Environment` + `o.Release` to the Sentry sink config.

**Why this matters:** without environment/release tags, Sentry events from prod/preprod/test all bucket together and you can't tell which release introduced an error. The two new env-vars (already in `.env.<env>.example` from Phase 00 Task 0.3) feed Sentry's grouping.

**Final state of the modified Sentry sink block:**

```cs
if (!string.IsNullOrWhiteSpace(sentryDsn))
{
    var sentryEnv = ctx.Configuration["SENTRY_ENVIRONMENT"]
                 ?? Environment.GetEnvironmentVariable("SENTRY_ENVIRONMENT")
                 ?? ctx.HostingEnvironment.EnvironmentName;
    var sentryRelease = ctx.Configuration["SENTRY_RELEASE"]
                     ?? Environment.GetEnvironmentVariable("SENTRY_RELEASE");

    cfg.WriteTo.Sentry(o =>
    {
        o.Dsn = sentryDsn;
        o.Environment = sentryEnv;
        if (!string.IsNullOrWhiteSpace(sentryRelease))
        {
            o.Release = sentryRelease;
        }
        o.MinimumEventLevel = LogEventLevel.Warning;
        o.MinimumBreadcrumbLevel = LogEventLevel.Information;
    });
}
```

**Note on fallback:** `Environment` falls back to `HostingEnvironment.EnvironmentName` (typically "Production" / "Development") if `SENTRY_ENVIRONMENT` isn't set, preserving Sub-10a behaviour. `Release` is optional — Sentry tolerates missing release tag.

- [ ] **Step 1:** Read current `LoggingExtensions.cs`:
  ```bash
  cat /Users/m/CCE/backend/src/CCE.Api.Common/Observability/LoggingExtensions.cs
  ```

- [ ] **Step 2:** Replace the existing Sentry sink block (the `if (!string.IsNullOrWhiteSpace(sentryDsn)) { ... }` clause) with the version above.

- [ ] **Step 3:** Build:
  ```bash
  cd /Users/m/CCE/backend && dotnet build src/CCE.Api.Common/ --nologo 2>&1 | tail -4
  ```
  Expected: success.

- [ ] **Step 4:** Commit:
  ```bash
  git -C /Users/m/CCE add backend/src/CCE.Api.Common/Observability/LoggingExtensions.cs
  git -C /Users/m/CCE -c commit.gpgsign=false commit -m "feat(api-common): SENTRY_ENVIRONMENT + SENTRY_RELEASE tags

  LoggingExtensions.UseCceSerilog reads SENTRY_ENVIRONMENT (falls
  back to HostingEnvironment.EnvironmentName) and SENTRY_RELEASE
  (optional) and propagates both to the Sentry sink config. Events
  from prod/preprod/test now bucket separately in Sentry; deploy
  tag (CCE_IMAGE_TAG) is recorded as the release version, making
  release-comparison grouping in Sentry's UI work correctly.

  Backwards-compatible: if neither env-var is set, Environment
  falls back to ASPNETCORE_ENVIRONMENT and Release is omitted
  (Sub-10a behaviour preserved).

  Sub-10c Phase 04 Task 4.2.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Task 4.3: `LoggingExtensionsSentryTests` — env + release flow into Sentry options

**Files:**
- Create: `backend/tests/CCE.Infrastructure.Tests/Observability/LoggingExtensionsSentryTests.cs` — verifies `SENTRY_ENVIRONMENT` + `SENTRY_RELEASE` are passed through to the Sentry sink's options.

**Why this test design:** the Sentry sink doesn't expose a public way to inspect its options post-build. The tests instead verify the behaviour by:
1. Building a host with the extension.
2. Reading the configured Sentry options indirectly — specifically, by substituting a fake DSN and asserting that `LogEventLevel` filtering + the env-var read paths are correct via reflection of the Serilog logger's sinks. Or simpler: assert that the configuration values propagate by exposing a thin `BuildSentryOptions` helper extracted from the inline lambda.

**Approach: extract testable helper.** Refactor `LoggingExtensions.UseCceSerilog` so the Sentry-options construction lives in a public-internal method that tests can invoke directly with a fake `IConfiguration`. The host-binding code stays in the public method; the option-building is the unit.

**Refactored `LoggingExtensions.cs`:**

```cs
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sentry.Serilog;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace CCE.Api.Common.Observability;

public static class LoggingExtensions
{
    public static IHostBuilder UseCceSerilog(this IHostBuilder builder)
    {
        return builder.UseSerilog((ctx, cfg) =>
        {
            var minLevel = ParseLevel(ctx.Configuration["Serilog:MinimumLevel"])
                ?? LogEventLevel.Information;
            var fileEnabled = ctx.Configuration.GetValue<bool>("Serilog:FileSink:Enabled");
            var filePath = ctx.Configuration["Serilog:FileSink:Path"] ?? "logs/cce-.log";
            var retainedDays = ctx.Configuration.GetValue<int?>("Serilog:FileSink:RetainedDays") ?? 7;
            var sentryDsn = ctx.Configuration["SENTRY_DSN"]
                         ?? Environment.GetEnvironmentVariable("SENTRY_DSN");

            cfg
                .MinimumLevel.Is(minLevel)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("app", ctx.HostingEnvironment.ApplicationName)
                .Enrich.WithProperty("env", ctx.HostingEnvironment.EnvironmentName)
                .WriteTo.Console(new CompactJsonFormatter());

            if (fileEnabled)
            {
                cfg.WriteTo.File(
                    formatter: new CompactJsonFormatter(),
                    path: filePath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: retainedDays);
            }

            if (!string.IsNullOrWhiteSpace(sentryDsn))
            {
                cfg.WriteTo.Sentry(o => ConfigureSentry(o, sentryDsn, ctx.Configuration, ctx.HostingEnvironment.EnvironmentName));
            }
        });
    }

    /// <summary>
    /// Public for testability. Constructs the Sentry sink options from
    /// the same configuration keys UseCceSerilog reads. Kept narrow so
    /// tests can verify env-var propagation without booting a host.
    /// </summary>
    public static void ConfigureSentry(
        SentrySerilogOptions options,
        string dsn,
        IConfiguration configuration,
        string fallbackEnvironmentName)
    {
        options.Dsn = dsn;
        options.Environment = configuration["SENTRY_ENVIRONMENT"]
                           ?? Environment.GetEnvironmentVariable("SENTRY_ENVIRONMENT")
                           ?? fallbackEnvironmentName;
        var release = configuration["SENTRY_RELEASE"]
                   ?? Environment.GetEnvironmentVariable("SENTRY_RELEASE");
        if (!string.IsNullOrWhiteSpace(release))
        {
            options.Release = release;
        }
        options.MinimumEventLevel = LogEventLevel.Warning;
        options.MinimumBreadcrumbLevel = LogEventLevel.Information;
    }

    private static LogEventLevel? ParseLevel(string? value)
        => Enum.TryParse<LogEventLevel>(value, ignoreCase: true, out var lvl) ? lvl : null;
}
```

**Final state of `LoggingExtensionsSentryTests.cs`:**

```cs
using CCE.Api.Common.Observability;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Sentry.Serilog;
using Serilog.Events;
using Xunit;

namespace CCE.Infrastructure.Tests.Observability;

public sealed class LoggingExtensionsSentryTests
{
    [Fact]
    public void ConfigureSentry_PropagatesEnvironmentFromConfig()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SENTRY_ENVIRONMENT"] = "production",
                ["SENTRY_RELEASE"]     = "app-v1.0.0",
            })
            .Build();
        var options = new SentrySerilogOptions();

        LoggingExtensions.ConfigureSentry(options, "https://x@y/1", config, fallbackEnvironmentName: "Production");

        options.Dsn.Should().Be("https://x@y/1");
        options.Environment.Should().Be("production");
        options.Release.Should().Be("app-v1.0.0");
        options.MinimumEventLevel.Should().Be(LogEventLevel.Warning);
        options.MinimumBreadcrumbLevel.Should().Be(LogEventLevel.Information);
    }

    [Fact]
    public void ConfigureSentry_FallsBackToHostingEnvironmentName_WhenSentryEnvironmentMissing()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var options = new SentrySerilogOptions();

        LoggingExtensions.ConfigureSentry(options, "https://x@y/1", config, fallbackEnvironmentName: "Staging");

        options.Environment.Should().Be("Staging");
    }

    [Fact]
    public void ConfigureSentry_LeavesReleaseUnset_WhenSentryReleaseMissing()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SENTRY_ENVIRONMENT"] = "test",
            })
            .Build();
        var options = new SentrySerilogOptions();

        LoggingExtensions.ConfigureSentry(options, "https://x@y/1", config, fallbackEnvironmentName: "Production");

        options.Environment.Should().Be("test");
        options.Release.Should().BeNull("Release should remain unset when SENTRY_RELEASE is not configured");
    }

    [Fact]
    public void ConfigureSentry_LeavesReleaseUnset_WhenSentryReleaseIsEmpty()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SENTRY_RELEASE"] = "",
            })
            .Build();
        var options = new SentrySerilogOptions();

        LoggingExtensions.ConfigureSentry(options, "https://x@y/1", config, fallbackEnvironmentName: "Production");

        options.Release.Should().BeNull();
    }
}
```

**Note on test project reference:** the tests need to reference `CCE.Api.Common`. Check whether `CCE.Infrastructure.Tests.csproj` already references it (transitively via `CCE.Infrastructure`); if not, add the project reference.

- [ ] **Step 1:** Refactor `LoggingExtensions.cs` to extract `ConfigureSentry` per the diff above.

- [ ] **Step 2:** Verify `CCE.Api.Common` reference is available in the test project:
  ```bash
  grep -i "api.common\|api\.common" /Users/m/CCE/backend/tests/CCE.Infrastructure.Tests/CCE.Infrastructure.Tests.csproj
  ```
  If not present, add `<ProjectReference Include="..\..\src\CCE.Api.Common\CCE.Api.Common.csproj" />` to the test csproj.

- [ ] **Step 3:** Create the test directory + file:
  ```bash
  mkdir -p /Users/m/CCE/backend/tests/CCE.Infrastructure.Tests/Observability
  ```

- [ ] **Step 4:** Create `Observability/LoggingExtensionsSentryTests.cs` with the contents above.

- [ ] **Step 5:** Build:
  ```bash
  cd /Users/m/CCE/backend && dotnet build tests/CCE.Infrastructure.Tests/ --nologo 2>&1 | tail -8
  ```
  Expected: success.

- [ ] **Step 6:** Run new tests:
  ```bash
  cd /Users/m/CCE/backend && dotnet test tests/CCE.Infrastructure.Tests/ --filter "FullyQualifiedName~Observability" --nologo 2>&1 | tail -5
  ```
  Expected: 4 passing.

- [ ] **Step 7:** Run full Infrastructure suite:
  ```bash
  cd /Users/m/CCE/backend && dotnet test tests/CCE.Infrastructure.Tests/ --nologo 2>&1 | tail -3
  ```
  Expected: 71 + 4 = 75 passing (1 skipped).

- [ ] **Step 8:** Commit:
  ```bash
  git -C /Users/m/CCE add backend/src/CCE.Api.Common/Observability/LoggingExtensions.cs \
                          backend/tests/CCE.Infrastructure.Tests/Observability/LoggingExtensionsSentryTests.cs \
                          backend/tests/CCE.Infrastructure.Tests/CCE.Infrastructure.Tests.csproj
  git -C /Users/m/CCE -c commit.gpgsign=false commit -m "test(observability): SENTRY_ENVIRONMENT + SENTRY_RELEASE flow

  LoggingExtensions.ConfigureSentry extracted as a public static
  helper so tests can verify env-var propagation without booting
  a host. UseCceSerilog now delegates to it.

  Four tests cover:
   - SENTRY_ENVIRONMENT + SENTRY_RELEASE both set; both propagate.
   - SENTRY_ENVIRONMENT missing; falls back to HostingEnvironmentName.
   - SENTRY_RELEASE missing; Release stays null.
   - SENTRY_RELEASE empty string; Release stays null.

  Adds CCE.Api.Common project reference to CCE.Infrastructure.Tests
  (if not already present).

  Infrastructure.Tests now 75 passing + 1 skipped.
  Sub-10c Phase 04 Task 4.3.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Task 4.4: Sentry alert rules section in deploy runbook

**Files:**
- Modify: `docs/runbooks/deploy.md` — add a new "Sentry alert rules" section documenting the recommended baseline alerts to configure in Sentry's UI.

**Why doc-only:** Sentry alert rules are configured in Sentry's web UI (org admin permission required). Sub-10c can't automate this; a runbook checklist is the correct artifact.

**New section to append to `docs/runbooks/deploy.md`** (after the existing "Logs" section):

```markdown
## Sentry alert rules (recommended baseline)

Sentry org admin configures these in the Sentry web UI per environment. The CCE backend tags every event with `environment` (production / preprod / test / dr) and `release` (`CCE_IMAGE_TAG`), making per-env alerting trivial.

### Recommended rules per project

| Rule | Trigger | Action |
|---|---|---|
| **First-occurrence in production** | New issue seen for the first time, environment = production | Email to ops on-call |
| **High error rate** | More than 1% of events in production are errors over 5 minutes | Email to ops on-call |
| **Auto-rollback triggered** | Event with tag `deploy.auto_rollback = true` (deploy.ps1 sends this) | Email + Slack/PagerDuty alert; treat as a real-time signal |
| **Regression after release** | Issue marked resolved seen again in a newer release | Email to dev team |

### Configuration steps

1. Sentry → Project → Alerts → Create Alert.
2. Pick "Issues" or "Metric Alert" depending on the rule.
3. Set conditions per the table above.
4. For environment-scoped alerts: under "If" filters, add `environment` equals the target environment.
5. For auto-rollback alerts: filter on `deploy.auto_rollback` tag (set by `deploy.ps1`).

### Verification

After alert rules are configured, fire a test event:

```powershell
# From the backend host: trigger a test exception (any endpoint that returns 500
# will do; or use Sentry's test-event endpoint).
curl -X POST https://api.CCE/admin/__test-error__ -H "Authorization: Bearer <token>"
```

Verify the event appears in Sentry within ~30 seconds + the alert fires per the rule.

### Auto-rollback events

Whenever `deploy.ps1` triggers an auto-rollback (smoke probe failed and `AUTO_ROLLBACK=true`), it POSTs a Sentry event tagged `deploy.auto_rollback = true` with:
- `environment` = `SENTRY_ENVIRONMENT` from the env-file
- `release` = the failed `CCE_IMAGE_TAG` (the bad tag, not the rolled-back-to tag)
- `tags.deploy.from_tag` = bad tag
- `tags.deploy.to_tag` = previous good tag
- `extra.reason` = "smoke-probe failure" (or future failure reasons)
- `extra.host` = `$env:COMPUTERNAME`

Use these tags in the alert filter for the "Auto-rollback triggered" rule.
```

- [ ] **Step 1:** Read existing `docs/runbooks/deploy.md` to find the right insertion point (after "## Logs"):
  ```bash
  grep -n "^## " /Users/m/CCE/docs/runbooks/deploy.md
  ```

- [ ] **Step 2:** Insert the new section above. Edit the existing file with the diff.

- [ ] **Step 3:** Commit:
  ```bash
  git -C /Users/m/CCE add docs/runbooks/deploy.md
  git -C /Users/m/CCE -c commit.gpgsign=false commit -m "docs(runbook): Sentry alert rules baseline + auto-rollback events

  Appends 'Sentry alert rules' section to deploy.md. Documents the
  recommended baseline rules per Sentry project (first-occurrence,
  high error rate, auto-rollback triggered, regression). Sub-10c
  doesn't automate Sentry alert provisioning; this is the operator
  click-path.

  Documents the auto-rollback event tags + extras that deploy.ps1
  POSTs (deploy.auto_rollback, deploy.from_tag, deploy.to_tag,
  reason, host) so operators can configure precise alert filters.

  Sub-10c Phase 04 Task 4.4.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Phase 04 close-out

After Task 4.4 commits cleanly:

- [ ] **Run the full check:**
  ```bash
  cd /Users/m/CCE/backend && dotnet build && \
    dotnet test tests/CCE.Application.Tests/ tests/CCE.Infrastructure.Tests/ --nologo
  ```
  Expected: backend build clean; 439 Application + 75 Infrastructure tests passing (1 skipped).

- [ ] **Verify CI green** on push.

- [ ] **Hand off to Phase 05.** Phase 05 ships the DR provisioning checklist + DR-promotion runbook + ADR-0057 + completion doc + CHANGELOG + tag `infra-v1.0.0`. Plan file: `phase-05-dr-and-closeout.md` (to be written when ready).

**Phase 04 done when:**
- 4 commits land on `main`, each green.
- `deploy.ps1` honours `-AutoRollback` / `-NoAutoRollback` / env-file `AUTO_ROLLBACK` / `-Recursive` precedence.
- Auto-rollback resolves previous tag from `deploy-history-${env}.tsv` and invokes `rollback.ps1`.
- `Send-SentryBreadcrumb` POSTs an event tagged `deploy.auto_rollback` (best-effort; no-op without DSN).
- `LoggingExtensions.ConfigureSentry` propagates `SENTRY_ENVIRONMENT` + `SENTRY_RELEASE` to Sentry sink options.
- 4 new Sentry-flow unit tests pass.
- Test counts: backend Application 439 (unchanged); Infrastructure 75 (was 71, +4 Sentry tests). Frontend 502.
- ADR + Sentry alert-rules section in deploy runbook committed.
