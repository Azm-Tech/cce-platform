using CCE.Api.Common.Auth;
using CCE.Api.Common.OpenApi;
using CCE.Application;
using CCE.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddCceJwtAuth(builder.Configuration)
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

app.Run();

namespace CCE.Api.Internal
{
    public partial class Program;
}
