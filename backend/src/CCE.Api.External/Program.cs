using CCE.Api.Common.OpenApi;
using CCE.Application;
using CCE.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddCceOpenApi("CCE External API");

var app = builder.Build();

app.UseCceOpenApi();
app.MapGet("/", () => "CCE.Api.External — Foundation");

app.Run();

namespace CCE.Api.External
{
    public partial class Program;
}
