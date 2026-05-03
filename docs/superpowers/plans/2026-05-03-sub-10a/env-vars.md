# Sub-10a — Environment variables

| Variable | Used by | Maps to / effect |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | both APIs | `Production` in prod images |
| `ASPNETCORE_URLS` | both APIs | `http://+:8080` in prod images |
| `ConnectionStrings__Default` | both APIs | SQL Server connection |
| `ConnectionStrings__Redis` | both APIs | Redis connection (cache) |
| `ASSISTANT_PROVIDER` | `Api.External` | overrides `Assistant:Provider`. Values: `stub` (default) or `anthropic`. |
| `ANTHROPIC_API_KEY` | `Api.External` | required when `ASSISTANT_PROVIDER=anthropic`. Absent → factory falls back to stub with a warn log. |
| `SENTRY_DSN` | both APIs | optional. Absent → Sentry sink is a no-op. |
| `LOG_LEVEL` | both APIs | overrides `Serilog:MinimumLevel`. Default `Information`. |

**Never committed:** `ANTHROPIC_API_KEY`, `SENTRY_DSN`. These are supplied at runtime via the deploy environment (Sub-10b will document the secret-supply mechanism).

**Defaults:** every variable has a safe default. CI runs without any of them set.
