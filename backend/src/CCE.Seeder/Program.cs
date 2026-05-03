using CCE.Application;
using CCE.Infrastructure;
using CCE.Infrastructure.Persistence;
using CCE.Seeder;
using CCE.Seeder.Seeders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Tiny console runner that bootstraps DI and dispatches based on CLI flags.
//   (no flag)              → RunSeeders          — existing dev seeder behaviour (no demo)
//   --demo                 → RunSeedersWithDemo  — seeders + demo data
//   --migrate              → MigrateOnly         — Database.MigrateAsync(), then exit
//   --migrate --seed-reference → MigrateAndSeedReference — migrate, then idempotent reference seeders
// Reads the same appsettings as CCE.Api.External so the connection string +
// Infrastructure section line up.

var mode = SeederMode.Parse(args);
if (mode.Mode == SeederMode.Kind.Error)
{
    await Console.Error.WriteLineAsync($"error: {mode.ErrorMessage}").ConfigureAwait(false);
    return 1;
}

// Walk up from the seeder's source directory to find the External API project's appsettings.
// We look for a directory that contains both `src/CCE.Api.External/appsettings.json` and `CCE.sln`
// (or similar marker), starting from AppContext.BaseDirectory and walking up until we find it.
static string FindApiAppSettingsDir()
{
    // Try the current working directory first (most reliable when run via `dotnet run --project ...`).
    var cwdCandidate = Path.Combine(Directory.GetCurrentDirectory(), "src", "CCE.Api.External");
    if (File.Exists(Path.Combine(cwdCandidate, "appsettings.json")))
    {
        return cwdCandidate;
    }

    // Fall back to walking up from BaseDirectory until we find the backend/src/CCE.Api.External folder.
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null)
    {
        var candidate = Path.Combine(dir.FullName, "src", "CCE.Api.External", "appsettings.json");
        if (File.Exists(candidate))
        {
            return Path.GetDirectoryName(candidate)!;
        }
        dir = dir.Parent;
    }

    throw new DirectoryNotFoundException(
        "Unable to locate src/CCE.Api.External/appsettings.json from either the current working directory or AppContext.BaseDirectory.");
}

var apiAppSettingsDir = FindApiAppSettingsDir();

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration
    .SetBasePath(apiAppSettingsDir)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables();

// AddApplication wires up MediatR, which the DomainEventDispatcher interceptor inside
// AddInfrastructure depends on (it resolves IPublisher).
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Register seeders.
builder.Services.AddScoped<ISeeder, ReferenceDataSeeder>();
builder.Services.AddScoped<ISeeder, RolesAndPermissionsSeeder>();
builder.Services.AddScoped<ISeeder, KnowledgeMapSeeder>();
builder.Services.AddScoped<ISeeder, DemoDataSeeder>();
builder.Services.AddScoped<SeedRunner>();

using var host = builder.Build();

using var scope = host.Services.CreateScope();
var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

switch (mode.Mode)
{
    case SeederMode.Kind.MigrateOnly:
    case SeederMode.Kind.MigrateAndSeedReference:
        var ctx = scope.ServiceProvider.GetRequiredService<CceDbContext>();
        logger.LogInformation("Applying EF Core migrations…");
        var pending = await ctx.Database.GetPendingMigrationsAsync().ConfigureAwait(false);
        var pendingList = pending.ToList();
        if (pendingList.Count == 0)
        {
            logger.LogInformation("No pending migrations.");
        }
        else
        {
            foreach (var name in pendingList)
            {
                logger.LogInformation("→ pending: {Migration}", name);
            }
            await ctx.Database.MigrateAsync().ConfigureAwait(false);
            logger.LogInformation("Applied {Count} migration(s).", pendingList.Count);
        }

        if (mode.Mode == SeederMode.Kind.MigrateAndSeedReference)
        {
            logger.LogInformation("Running idempotent reference-data seeders (RolesAndPermissions, ReferenceData, KnowledgeMap)…");
            // The four seeders are registered by Order; demo (Order=100) is excluded
            // by passing includeDemo:false. RunAllAsync already orders ascending.
            var runner = scope.ServiceProvider.GetRequiredService<SeedRunner>();
            await runner.RunAllAsync(includeDemo: false).ConfigureAwait(false);
        }
        return 0;

    case SeederMode.Kind.RunSeedersWithDemo:
        logger.LogInformation("Starting seeder (demo=true).");
        var demoRunner = scope.ServiceProvider.GetRequiredService<SeedRunner>();
        await demoRunner.RunAllAsync(includeDemo: true).ConfigureAwait(false);
        logger.LogInformation("Seeder finished.");
        return 0;

    default: // RunSeeders
        logger.LogInformation("Starting seeder (demo=false).");
        var noDemoRunner = scope.ServiceProvider.GetRequiredService<SeedRunner>();
        await noDemoRunner.RunAllAsync(includeDemo: false).ConfigureAwait(false);
        logger.LogInformation("Seeder finished.");
        return 0;
}
