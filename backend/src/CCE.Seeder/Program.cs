using CCE.Application;
using CCE.Infrastructure;
using CCE.Seeder;
using CCE.Seeder.Seeders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Tiny console runner that bootstraps DI and runs all registered ISeeders.
// Reads the same appsettings as CCE.Api.External so the connection string +
// Infrastructure section line up. Pass --demo to include DemoDataSeeder.

var includeDemo = args.Contains("--demo", StringComparer.OrdinalIgnoreCase);

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

using (var scope = host.Services.CreateScope())
{
    var runner = scope.ServiceProvider.GetRequiredService<SeedRunner>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Starting seeder (demo={Demo})", includeDemo);
    await runner.RunAllAsync(includeDemo: includeDemo).ConfigureAwait(false);
    logger.LogInformation("Seeder finished.");
}
