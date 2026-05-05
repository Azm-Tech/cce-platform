using CCE.Api.Common.Auth;
using CCE.Api.Common.Authorization;
using CCE.Api.Common.Health;
using CCE.Api.Common.Identity;
using CCE.Api.Common.Middleware;
using CCE.Api.Common.Observability;
using CCE.Api.Common.OpenApi;
using CCE.Api.Common.RateLimiting;
using CCE.Api.Internal.Endpoints;
using CCE.Application;
using CCE.Application.Common.CountryScope;
using CCE.Application.Common.Interfaces;
using CCE.Application.Health;
using CCE.Infrastructure;
using CCE.Infrastructure.Search;
using MediatR;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseCceSerilog();

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddCceMeilisearchIndexer()
    .AddCceJwtAuth(builder.Configuration)
    .AddCcePermissionPolicies()
    .AddCceUserSync()
    .AddCceHealthChecks(builder.Configuration)
    .AddCceRateLimiter(builder.Configuration)
    .AddCceOpenApi("CCE Internal API");

builder.Services.AddHttpContextAccessor();
builder.Services.Replace(ServiceDescriptor.Scoped<ICurrentUserAccessor, HttpContextCurrentUserAccessor>());
builder.Services.Replace(ServiceDescriptor.Scoped<ICountryScopeAccessor, HttpContextCountryScopeAccessor>());

var app = builder.Build();

// Middleware order (spec §7.1): correlation → exception → security headers → auth → authz → rate → locale
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseCceUserSync();
app.UseRateLimiter();
app.UseCcePrometheus();
app.UseMiddleware<LocalizationMiddleware>();

app.UseCceOpenApi(apiTag: "internal");

app.MapIdentityEndpoints();
app.MapExpertEndpoints();
app.MapAssetEndpoints();
app.MapResourceEndpoints();
app.MapResourceCategoryEndpoints();
app.MapCountryResourceRequestEndpoints();
app.MapCountryEndpoints();
app.MapCountryProfileEndpoints();
app.MapNewsEndpoints();
app.MapEventEndpoints();
app.MapPageEndpoints();
app.MapHomepageSectionEndpoints();
app.MapTopicEndpoints();
app.MapCommunityModerationEndpoints();
app.MapNotificationTemplateEndpoints();
app.MapReportEndpoints();
app.MapAuditEndpoints();

// Sub-11d follow-up — dev sign-in shim. Mounts /dev/sign-in,
// /dev/sign-out, /dev/whoami when Auth:DevMode=true. Production
// deployments leave the flag false → endpoints are never mounted.
if (builder.Configuration.GetValue<bool>("Auth:DevMode"))
{
    app.MapDevAuthEndpoints();
}

app.MapGet("/", () => "CCE.Api.Internal — Foundation");

app.MapGet("/auth/echo", (HttpContext ctx) =>
{
    var name = ctx.User.Identity?.Name ?? "(no name)";
    var upn = ctx.User.FindFirst("upn")?.Value ?? "(no upn)";
    return Results.Ok(new { name, upn });
}).RequireAuthorization();

app.MapGet("/health", async (IMediator mediator) =>
{
    var locale = CultureInfo.CurrentCulture.Name;
    var result = await mediator.Send(new HealthQuery(Locale: locale)).ConfigureAwait(false);
    return Results.Ok(result);
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapGet("/health/authenticated", async (IMediator mediator, HttpContext ctx) =>
{
    var user = ctx.User;
    var groups = user.FindAll("groups").Select(c => c.Value).ToList();
    var locale = CultureInfo.CurrentCulture.Name;

    var query = new AuthenticatedHealthQuery(
        UserId: user.FindFirst("sub")?.Value ?? "(no sub)",
        PreferredUsername: user.FindFirst("preferred_username")?.Value ?? "(no name)",
        Email: user.FindFirst("email")?.Value ?? "(no email)",
        Upn: user.FindFirst("upn")?.Value ?? "(no upn)",
        Groups: groups,
        Locale: locale);

    var result = await mediator.Send(query).ConfigureAwait(false);
    return Results.Ok(result);
})
.RequireAuthorization(policy => policy.RequireClaim("groups", "SuperAdmin"));

app.Run();

namespace CCE.Api.Internal
{
    public partial class Program;
}
