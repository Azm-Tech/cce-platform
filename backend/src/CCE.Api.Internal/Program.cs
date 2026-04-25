using CCE.Api.Common.OpenApi;
using CCE.Application;
using CCE.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddCceOpenApi("CCE Internal API");

var app = builder.Build();

app.UseCceOpenApi();
app.MapGet("/", () => "CCE.Api.Internal — Foundation");

app.Run();

namespace CCE.Api.Internal
{
    public partial class Program;
}
