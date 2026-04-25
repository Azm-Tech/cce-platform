using CCE.Application;
using CCE.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.MapGet("/", () => "CCE.Api.Internal — Foundation");

app.Run();

public partial class Program;
