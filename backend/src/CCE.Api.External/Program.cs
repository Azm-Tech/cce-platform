using CCE.Api.Common.Auth;
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
    .AddCceOpenApi("CCE External API");

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<CCE.Api.Common.Middleware.LocalizationMiddleware>();
app.UseCceOpenApi();

app.MapGet("/", () => "CCE.Api.External — Foundation");

app.MapGet("/health", async (IMediator mediator) =>
{
    var locale = System.Globalization.CultureInfo.CurrentCulture.Name;
    var result = await mediator.Send(new HealthQuery(Locale: locale)).ConfigureAwait(false);
    return Results.Ok(result);
});

app.MapGet("/auth/echo", (HttpContext ctx) =>
{
    var name = ctx.User.Identity?.Name ?? "(no name)";
    var upn = ctx.User.FindFirst("upn")?.Value ?? "(no upn)";
    return Results.Ok(new { name, upn });
}).RequireAuthorization();

app.Run();

namespace CCE.Api.External
{
    public partial class Program;
}
