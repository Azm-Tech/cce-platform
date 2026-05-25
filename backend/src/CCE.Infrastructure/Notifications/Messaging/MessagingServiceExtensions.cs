using CCE.Application.Notifications.Messages;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CCE.Infrastructure.Notifications.Messaging;

/// <summary>
/// Registers MassTransit with the correct transport based on
/// <c>appsettings.json → Messaging:Transport</c>:
///
/// <list type="table">
///   <item><term>InMemory</term><description>No broker. Messages flow in-process via a channel. Use for local dev and all tests.</description></item>
///   <item><term>RabbitMQ</term><description>Production. Requires <c>Messaging:RabbitMqHost</c> and a running broker.</description></item>
/// </list>
///
/// Call <c>services.AddCceMessaging(configuration)</c> from
/// <see cref="CCE.Infrastructure.DependencyInjection.AddInfrastructure"/>.
/// </summary>
public static class MessagingServiceExtensions
{
    public static IServiceCollection AddCceMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<MessagingOptions>()
            .Bind(configuration.GetSection(MessagingOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var options = configuration
            .GetSection(MessagingOptions.SectionName)
            .Get<MessagingOptions>() ?? new MessagingOptions();

        services.AddMassTransit(x =>
        {
            // Register consumer + its definition (retry policy, concurrency).
            x.AddConsumer<NotificationMessageConsumer, NotificationMessageConsumerDefinition>();

            switch (options.Transport.ToUpperInvariant())
            {
                case "RABBITMQ":
                    x.UsingRabbitMq((ctx, cfg) =>
                    {
                        cfg.Host(options.RabbitMqHost ?? "amqp://guest:guest@localhost", options.RabbitMqVirtualHost, h =>
                        {
                            // Credentials are embedded in RabbitMqHost URI or set here.
                            // Production: use environment variables / Azure Key Vault secrets.
                        });

                        // Auto-configure endpoints from consumer definitions.
                        cfg.ConfigureEndpoints(ctx);
                    });
                    break;

                default: // "InMemory" or missing
                    x.UsingInMemory((ctx, cfg) =>
                    {
                        cfg.ConfigureEndpoints(ctx);
                    });
                    break;
            }
        });

        // Replace the synchronous in-process dispatcher with the async bus publisher
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
}
