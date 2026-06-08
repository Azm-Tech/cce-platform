using System.ComponentModel.DataAnnotations;
using CCE.Application.Notifications.Messages;

namespace CCE.Infrastructure.Notifications.Messaging;

/// <summary>
/// Bound from <c>appsettings.json</c> section <c>"Messaging"</c>.
/// </summary>
public sealed class MessagingOptions
{
    public const string SectionName = "Messaging";

    /// <summary>
    /// Transport to use.
    /// <list type="bullet">
    ///   <item><c>InMemory</c> — default; same process, no broker required (dev / test).</item>
    ///   <item><c>RabbitMQ</c> — production; requires <see cref="RabbitMqHost"/> config.</item>
    /// </list>
    /// </summary>
    [Required]
    public string Transport { get; init; } = "InMemory";

    /// <summary>RabbitMQ host URI, e.g. <c>amqp://localhost</c> or <c>rabbitmq</c> (host name).
    /// Credentials should be supplied via <see cref="RabbitMqUsername"/>/<see cref="RabbitMqPassword"/>
    /// (env vars in production) rather than embedded in this URI.</summary>
    public string? RabbitMqHost { get; init; }

    /// <summary>
    /// Virtual host inside RabbitMQ. Defaults to <c>"/"</c>.
    /// Use a dedicated vhost per environment (dev/staging/prod) to keep queues isolated.
    /// </summary>
    public string RabbitMqVirtualHost { get; init; } = "/";

    /// <summary>RabbitMQ username. Required when <see cref="Transport"/> is <c>RabbitMQ</c>.
    /// Supply via the <c>Messaging__RabbitMqUsername</c> env var in production — never commit it.</summary>
    public string? RabbitMqUsername { get; init; }

    /// <summary>RabbitMQ password. Required when <see cref="Transport"/> is <c>RabbitMQ</c>.
    /// Supply via the <c>Messaging__RabbitMqPassword</c> env var in production — never commit it.</summary>
    public string? RabbitMqPassword { get; init; }

    /// <summary>
    /// <strong>Dev convenience only.</strong> When <c>true</c> and <see cref="Transport"/> is
    /// <c>RabbitMQ</c>, a fast startup TCP probe checks broker reachability; if it fails the bus falls
    /// back to the InMemory transport (and consumers run in-process) instead of failing startup. Leave
    /// <c>false</c> in production: with the outbox in place a transient broker outage is already handled
    /// (host starts, MassTransit auto-reconnects, messages wait durably in <c>outbox_message</c>), and a
    /// real outage should surface via the readiness health check rather than being silently masked.
    /// </summary>
    public bool FallbackToInMemoryIfUnavailable { get; init; }

    /// <summary>
    /// When <c>true</c> (default), <see cref="INotificationMessageDispatcher"/> is replaced
    /// with <see cref="MassTransitNotificationMessageDispatcher"/>. Set <c>false</c> to keep
    /// the synchronous in-process dispatcher even when MassTransit is registered
    /// (useful for integration tests that mock the gateway).
    /// </summary>
    public bool UseAsyncDispatcher { get; init; } = true;
}
