using System.ComponentModel.DataAnnotations;

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

    /// <summary>RabbitMQ host URI, e.g. <c>amqp://guest:guest@localhost</c>.</summary>
    public string? RabbitMqHost { get; init; }

    /// <summary>
    /// Virtual host inside RabbitMQ. Defaults to <c>"/"</c>.
    /// Use a dedicated vhost per environment (dev/staging/prod) to keep queues isolated.
    /// </summary>
    public string RabbitMqVirtualHost { get; init; } = "/";

    /// <summary>
    /// When <c>true</c> (default), <see cref="INotificationMessageDispatcher"/> is replaced
    /// with <see cref="MassTransitNotificationMessageDispatcher"/>. Set <c>false</c> to keep
    /// the synchronous in-process dispatcher even when MassTransit is registered
    /// (useful for integration tests that mock the gateway).
    /// </summary>
    public bool UseAsyncDispatcher { get; init; } = true;
}
