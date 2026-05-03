using CCE.Api.Common.Auth;
using CCE.Api.Common.Authorization;
using CCE.Api.Common.Caching;
using CCE.Api.Common.Health;
using CCE.Api.Common.Identity;
using CCE.Api.Common.Middleware;
using CCE.Api.Common.Observability;
using CCE.Api.Common.OpenApi;
using CCE.Api.Common.RateLimiting;
using CCE.Api.External.Endpoints;
using CCE.Application;
using CCE.Application.Common.CountryScope;
using CCE.Application.Common.Interfaces;
using CCE.Application.Health;
using CCE.Infrastructure;
using MediatR;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog;
using System.Globalization;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Wire Serilog (console JSON + rolling file + optional Sentry sink).
// Reads Serilog:* config from appsettings + SENTRY_DSN env-var.
builder.Host.UseCceSerilog();

// Serialize enums as their member names (e.g. "Sector") rather than the underlying int.
// The TypeScript types in apps/web-portal already declare every enum field as a string union
// (e.g. NodeType = 'Technology' | 'Sector' | 'SubTopic'), so without this converter the
// Cytoscape stylesheet selectors and ListViewComponent grouping silently fail at runtime.
builder.Services.ConfigureHttpJsonOptions(opts =>
{
    opts.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddCceBff(builder.Configuration)
    .AddCceOutputCache(builder.Configuration)
    .AddCceTieredRateLimiter(builder.Configuration)
    .AddCceJwtAuth(builder.Configuration)
    .AddCcePermissionPolicies()
    .AddCceUserSync()
    .AddCceHealthChecks(builder.Configuration)
    .AddCceOpenApi("CCE External API");

builder.Services.AddHttpContextAccessor();
builder.Services.Replace(ServiceDescriptor.Scoped<ICurrentUserAccessor, HttpContextCurrentUserAccessor>());
builder.Services.Replace(ServiceDescriptor.Scoped<ICountryScopeAccessor, HttpContextCountryScopeAccessor>());

var app = builder.Build();

// Middleware order (spec §7.1): correlation → exception → security headers → rate → auth → output-cache → authz → locale
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseCceTieredRateLimiter();
app.UseCceBff();
app.UseCceOutputCache();
app.UseAuthentication();
app.UseAuthorization();
app.UseCceUserSync();
app.UseCcePrometheus();
app.UseMiddleware<LocalizationMiddleware>();

app.UseCceOpenApi(apiTag: "external");

app.MapGet("/", () => "CCE.Api.External — Foundation");

app.MapGet("/auth/echo", (HttpContext ctx) =>
{
    var name = ctx.User.Identity?.Name ?? "(no name)";
    var upn = ctx.User.FindFirst("upn")?.Value ?? "(no upn)";
    return Results.Ok(new { name, upn });
}).RequireAuthorization();

app.MapBffAuthEndpoints();

app.MapProfileEndpoints();
app.MapNotificationsEndpoints();
app.MapNewsPublicEndpoints();
app.MapEventsPublicEndpoints();
app.MapResourcesPublicEndpoints();
app.MapPagesPublicEndpoints();
app.MapHomepageSectionsPublicEndpoints();
app.MapTopicsPublicEndpoints();
app.MapCategoriesPublicEndpoints();
app.MapCountriesPublicEndpoints();
app.MapSearchEndpoints();
app.MapCommunityPublicEndpoints();
app.MapCommunityWriteEndpoints();
app.MapKnowledgeMapEndpoints();
app.MapInteractiveCityEndpoints();
app.MapAssistantEndpoints();
app.MapKapsarcEndpoints();
app.MapSurveysEndpoints();

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

app.Run();

namespace CCE.Api.External
{
    public partial class Program;
}
