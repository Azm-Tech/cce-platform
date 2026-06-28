using CCE.Api.Common.Health;
using CCE.Api.Common.Observability;
using CCE.Api.Common.SignalR;
using CCE.Application;
using CCE.Infrastructure;
using Serilog;

// CCE.Worker — the consume side of the messaging topology.
//
// The APIs publish integration events / notifications into the transactional outbox; this worker hosts
// the MassTransit consumers (NotificationMessageConsumer + future consumers) and the bus-outbox delivery
// loop. It is a WebApplication (not a bare worker host) only so it can reuse CCE.Api.Common's Serilog,
// OpenTelemetry and health-check wiring and expose /health — it maps NO business endpoints.
var builder = WebApplication.CreateBuilder(args);

builder.Host.UseCceSerilog();

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration, registerConsumers: true)
    .AddCceHealthChecks(builder.Configuration)
    .AddCceOpenTelemetry(builder.Configuration, "CCE.Worker");

// IDataProtectionProvider is required by ASP.NET Identity's token provider.
// The Worker is a WebApplication for health-check reuse; it needs this explicitly
// because it does not call AddAuthentication/AddMvc like the APIs do.
builder.Services.AddDataProtection();

// The notification consumer resolves INotificationGateway, which transitively needs
// IHubContext<NotificationsHub> (the InApp realtime channel). AddCceSignalR registers that hub context
// AND wires the Redis backplane, so a notification pushed here fans out through Redis to the clients
// connected to the API instances (the worker itself serves no clients).
builder.Services.AddCceSignalR(builder.Configuration);

var app = builder.Build();

app.UseSerilogRequestLogging();

app.MapGet("/", () => "CCE.Worker — message consumers");

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();
