using CCE.Application.Notifications;
using CCE.Application.Notifications.Messages;
using CCE.Domain.Notifications;
using CCE.Infrastructure.Notifications.Messaging;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace CCE.Infrastructure.Tests.Messaging;

/// <summary>
/// Verifies the messaging round-trip with MassTransit's in-memory test harness: a published
/// <see cref="NotificationMessage"/> is delivered to <see cref="NotificationMessageConsumer"/>, which
/// hands it to <see cref="INotificationGateway"/>. Runs without a broker, SQL Server, or the outbox.
/// </summary>
public sealed class NotificationMessageConsumerHarnessTests
{
    [Fact]
    public async Task Published_NotificationMessage_is_consumed_and_forwarded_to_the_gateway()
    {
        var gateway = Substitute.For<INotificationGateway>();
        gateway
            .SendAsync(Arg.Any<NotificationDispatchRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(new NotificationDispatchResult(
                TemplateCode: "TEST_TEMPLATE",
                RecipientUserId: null,
                Results: System.Array.Empty<NotificationChannelDispatchResult>())));

        await using var provider = new ServiceCollection()
            .AddSingleton(gateway)
            .AddMassTransitTestHarness(x => x.AddConsumer<NotificationMessageConsumer>())
            .BuildServiceProvider(validateScopes: true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        try
        {
            var message = new NotificationMessage(
                TemplateCode: "TEST_TEMPLATE",
                RecipientUserId: System.Guid.NewGuid(),
                EventType: NotificationEventType.ResourcePublished,
                Channels: new[] { NotificationChannel.InApp },
                Locale: "en");

            await harness.Bus.Publish(message);

            (await harness.Consumed.Any<NotificationMessage>()).Should().BeTrue();

            var consumerHarness = harness.GetConsumerHarness<NotificationMessageConsumer>();
            (await consumerHarness.Consumed.Any<NotificationMessage>()).Should().BeTrue();

            await gateway.Received(1).SendAsync(
                Arg.Is<NotificationDispatchRequest>(r => r.TemplateCode == "TEST_TEMPLATE"),
                Arg.Any<CancellationToken>());
        }
        finally
        {
            await harness.Stop();
        }
    }
}
