using CCE.Api.Common.Auth;
using CCE.Api.Common.Health;
using CCE.Api.Common.OpenApi;
using CCE.Application;
using CCE.Application.Health;
using CCE.Infrastructure;
using MediatR;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddCceJwtAuth(builder.Configuration)
    .AddCceHealthChecks(builder.Configuration)
    .AddCceOpenApi("CCE Internal API");

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.UseCceOpenApi();

app.MapGet("/", () => "CCE.Api.Internal — Foundation");

app.MapGet("/auth/echo", (HttpContext ctx) =>
{
    var name = ctx.User.Identity?.Name ?? "(no name)";
    var upn = ctx.User.FindFirst("upn")?.Value ?? "(no upn)";
    return Results.Ok(new { name, upn });
}).RequireAuthorization();

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapGet("/health/authenticated", async (IMediator mediator, HttpContext ctx) =>
{
    var user = ctx.User;
    var groups = user.FindAll("groups").Select(c => c.Value).ToList();
    var locale = System.Globalization.CultureInfo.CurrentCulture.Name;

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
