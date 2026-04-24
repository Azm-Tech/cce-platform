using CCE.Application;
using CCE.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure();

var app = builder.Build();

app.MapGet("/", () => "CCE.Api.External — Foundation");

app.Run();

// Expose Program for WebApplicationFactory in integration tests
public partial class Program;
