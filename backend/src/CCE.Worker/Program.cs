using CCE.Api.Common.Health;
using CCE.Api.Common.Observability;
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

// The notification consumer resolves INotificationGateway, which transitively needs
// IHubContext<NotificationsHub> (the InApp realtime channel). AddSignalR registers that hub context so
// the DI graph is satisfiable in this process.
//
// NOTE (follow-up): realtime delivery to clients connected to the *APIs* requires a SignalR Redis
// backplane. Without one the worker's push is local-only; the in-app notification is still persisted to
// the database by the gateway, so clients see it on their next fetch — only the live push is missed.
builder.Services.AddSignalR();

var app = builder.Build();

app.UseSerilogRequestLogging();

app.MapGet("/", () => "CCE.Worker — message consumers");

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();
