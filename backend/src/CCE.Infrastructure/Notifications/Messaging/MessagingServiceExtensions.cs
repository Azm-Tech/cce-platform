using CCE.Application.Common.Messaging;
using CCE.Application.Notifications.Messages;
using CCE.Infrastructure.Notifications.Messaging.Consumers;
using CCE.Infrastructure.Persistence;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CCE.Infrastructure.Notifications.Messaging;

/// <summary>
/// Registers MassTransit with the EF Core transactional outbox and the correct transport based on
/// <c>appsettings.json → Messaging:Transport</c>:
///
/// <list type="table">
///   <item><term>InMemory</term><description>No broker. Messages flow in-process via a channel. Used for local dev and all tests.</description></item>
///   <item><term>RabbitMQ</term><description>Staging/production. Requires <c>Messaging:RabbitMqHost</c> + credentials and a running broker.</description></item>
/// </list>
///
/// <para>
/// The <c>registerConsumers</c> flag controls who runs receive endpoints: the APIs (and Seeder) call
/// with <c>false</c> (publish-only — they stage messages into the outbox), while <c>CCE.Worker</c>
/// calls with <c>true</c> to host the consumers. The <c>BusOutboxDeliveryService</c> (enabled by
/// <c>UseBusOutbox</c>) runs in every process and relays staged <c>outbox_message</c> rows to the bus.
/// </para>
///
/// <para>
/// Called from <see cref="CCE.Infrastructure.DependencyInjection.AddInfrastructure"/>.
/// </para>
/// </summary>
public static class MessagingServiceExtensions
{
    public static IServiceCollection AddCceMessaging(
        this IServiceCollection services,
        IConfiguration configuration,
        bool registerConsumers = false)
    {
        services.AddOptions<MessagingOptions>()
            .Bind(configuration.GetSection(MessagingOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var options = configuration
            .GetSection(MessagingOptions.SectionName)
            .Get<MessagingOptions>() ?? new MessagingOptions();

        var useRabbitMq = options.Transport.Equals("RabbitMQ", System.StringComparison.OrdinalIgnoreCase);

        // Dev-only fallback: if RabbitMQ is requested but unreachable, drop to InMemory rather than
        // failing startup. See MessagingOptions.FallbackToInMemoryIfUnavailable for why this is dev-only.
        if (useRabbitMq && options.FallbackToInMemoryIfUnavailable
            && !CanReachRabbitMq(options.RabbitMqHost, System.TimeSpan.FromSeconds(2)))
        {
            System.Console.WriteLine(
                $"[CCE.Messaging] RabbitMQ at '{options.RabbitMqHost ?? "(null)"}' is unreachable — " +
                "falling back to the InMemory transport (consumers will run in-process). " +
                "This fallback is dev-only; set Messaging:FallbackToInMemoryIfUnavailable=false in production.");
            useRabbitMq = false;
            // An InMemory bus is per-process, so the falling-back host must consume in-process to
            // keep messages flowing (restores the pre-Worker single-process behaviour).
            registerConsumers = true;
        }

        // An InMemory bus is per-process: messages published (and relayed out of the EF bus outbox)
        // only reach consumers hosted in the SAME process. So whenever the effective transport is
        // InMemory — whether configured directly or via the fallback above — the publishing process
        // MUST also host the consumers, otherwise the outbox delivery service stamps sent_time and
        // the message is dropped on a bus with no receive endpoints. (RabbitMQ is a shared broker, so
        // a separate CCE.Worker can consume there; InMemory has no such option.)
        if (!useRabbitMq)
            registerConsumers = true;

        services.AddMassTransit(x =>
        {
            // EF Core transactional outbox. Publishing through IPublishEndpoint while a CceDbContext is
            // in scope stages an outbox_message row in that context; it is committed by the same
            // SaveChanges as the aggregate (see DomainEventDispatcher), then relayed by the
            // BusOutboxDeliveryService. This is what makes async events crash-safe (no dual write).
            x.AddEntityFrameworkOutbox<CceDbContext>(o =>
            {
                o.QueryDelay = System.TimeSpan.FromSeconds(1);
                o.UseSqlServer();
                o.UseBusOutbox();
            });

            // Kebab-case queue/exchange names (e.g. notification-message). Set on the registration
            // configurator so both the RabbitMQ and InMemory transports use the same convention.
            x.SetKebabCaseEndpointNameFormatter();

            // Consumers run only where registerConsumers is true (CCE.Worker, or an in-process dev
            // fallback). Publish-only hosts (APIs/Seeder) skip them and just stage to the outbox.
            if (registerConsumers)
            {
                x.AddConsumer<NotificationMessageConsumer, NotificationMessageConsumerDefinition>();
                x.AddConsumer<FeedConsumer, FeedConsumerDefinition>();
                x.AddConsumer<VoteConsumer, VoteConsumerDefinition>();
                x.AddConsumer<RankingConsumer, RankingConsumerDefinition>();
                x.AddConsumer<NotificationConsumer, NotificationConsumerDefinition>();
                x.AddConsumer<ContentNotificationConsumer, ContentNotificationConsumerDefinition>();
                x.AddConsumer<SignalRConsumer, SignalRConsumerDefinition>();
            }

            if (useRabbitMq)
            {
                x.UsingRabbitMq((ctx, cfg) =>
                {
                    cfg.Host(options.RabbitMqHost ?? "localhost", options.RabbitMqVirtualHost, h =>
                    {
                        // Credentials come from config/env (Messaging__RabbitMqUsername/Password),
                        // never the committed appsettings or the host URI.
                        if (!string.IsNullOrWhiteSpace(options.RabbitMqUsername))
                            h.Username(options.RabbitMqUsername);
                        if (!string.IsNullOrWhiteSpace(options.RabbitMqPassword))
                            h.Password(options.RabbitMqPassword);
                    });

                    // Build receive endpoints from registered consumer definitions (no-op when none).
                    cfg.ConfigureEndpoints(ctx);
                });
            }
            else // "InMemory" (default), or RabbitMQ that fell back to InMemory in dev
            {
                x.UsingInMemory((ctx, cfg) =>
                {
                    cfg.ConfigureEndpoints(ctx);
                });
            }
        });

        // Async integration-event publisher (general bus abstraction used by the Application layer).
        services.AddScoped<IIntegrationEventPublisher, MassTransitIntegrationEventPublisher>();

        // Replace the synchronous in-process notification dispatcher with the async bus publisher
        // only when UseAsyncDispatcher=true (default).
        if (options.UseAsyncDispatcher)
        {
            // Remove the InProcessNotificationMessageDispatcher registered in DependencyInjection.cs
            var descriptor = services.FirstOrDefault(
                d => d.ServiceType == typeof(INotificationMessageDispatcher));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddScoped<INotificationMessageDispatcher,
                MassTransitNotificationMessageDispatcher>();
        }

        return services;
    }

    /// <summary>
    /// Fast TCP reachability probe for the RabbitMQ host (dev fallback only). Returns <c>false</c> on
    /// any failure within <paramref name="timeout"/>. Accepts a bare host, <c>host:port</c>, or an
    /// <c>amqp://user:pass@host:port</c> URI; defaults to port 5672.
    /// </summary>
#pragma warning disable CA1031 // A probe must treat *any* failure (DNS, refused, timeout) as "unreachable".
    private static bool CanReachRabbitMq(string? rabbitMqHost, System.TimeSpan timeout)
    {
        var (host, port) = ParseHostPort(rabbitMqHost);
        try
        {
            using var client = new System.Net.Sockets.TcpClient();
            return client.ConnectAsync(host, port).Wait(timeout) && client.Connected;
        }
        catch (System.Exception)
        {
            return false;
        }
    }
#pragma warning restore CA1031

    private static (string Host, int Port) ParseHostPort(string? rabbitMqHost)
    {
        const int defaultPort = 5672;
        if (string.IsNullOrWhiteSpace(rabbitMqHost))
            return ("localhost", defaultPort);

        if (rabbitMqHost.Contains("://", System.StringComparison.Ordinal)
            && System.Uri.TryCreate(rabbitMqHost, System.UriKind.Absolute, out var uri))
            return (uri.Host, uri.Port > 0 ? uri.Port : defaultPort);

        var parts = rabbitMqHost.Split(':');
        return parts.Length == 2 && int.TryParse(parts[1], out var p)
            ? (parts[0], p)
            : (rabbitMqHost, defaultPort);
    }
}
