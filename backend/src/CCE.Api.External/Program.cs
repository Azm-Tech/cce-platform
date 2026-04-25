using CCE.Api.Common.Auth;
using CCE.Api.Common.Authorization;
using CCE.Api.Common.Health;
using CCE.Api.Common.Middleware;
using CCE.Api.Common.OpenApi;
using CCE.Api.Common.RateLimiting;
using CCE.Application;
using CCE.Application.Health;
using CCE.Infrastructure;
using MediatR;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddCceJwtAuth(builder.Configuration)
    .AddCcePermissionPolicies()
    .AddCceHealthChecks(builder.Configuration)
    .AddCceRateLimiter()
    .AddCceOpenApi("CCE External API");

var app = builder.Build();

// Middleware order (spec §7.1): correlation → exception → security headers → auth → authz → rate → locale
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseMiddleware<LocalizationMiddleware>();

app.UseCceOpenApi();

app.MapGet("/", () => "CCE.Api.External — Foundation");

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

app.Run();

namespace CCE.Api.External
{
    public partial class Program;
}
