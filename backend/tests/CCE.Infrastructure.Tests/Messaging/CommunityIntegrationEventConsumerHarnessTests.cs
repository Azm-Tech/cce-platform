using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Messaging.IntegrationEvents;
using CCE.Application.Common.Realtime;
using CCE.Application.Community;
using CCE.Application.Notifications.Messages;
using CCE.Infrastructure.Notifications;
using CCE.Infrastructure.Notifications.Messaging.Consumers;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace CCE.Infrastructure.Tests.Messaging;

/// <summary>
/// Verifies the Worker-side consumer routing for the community integration events using MassTransit's
/// in-memory test harness — no broker, SQL Server, or outbox. Covers the contracts whose consumers have
/// no database dependency on the exercised path, which is enough to prove: the bus routes each event to
/// the right consumer; the contracts (including the added <c>PostCreatedIntegrationEvent.Locale</c>)
/// round-trip; the realtime dedup holds (VoteConsumer does the Redis update but no SignalR push); and the
/// post-notification fan-out now runs in <see cref="NotificationConsumer"/> rather than the API thread.
/// </summary>
public sealed class CommunityIntegrationEventConsumerHarnessTests
{
    [Fact]
    public async Task VoteCreated_updates_redis_counters_and_does_not_push_signalr()
    {
        var feedStore = Substitute.For<IRedisFeedStore>();

        await using var provider = new ServiceCollection()
            .AddLogging()
            .AddSingleton(feedStore)
            .AddMassTransitTestHarness(x => x.AddConsumer<VoteConsumer>())
            .BuildServiceProvider(validateScopes: true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        try
        {
            var postId = System.Guid.NewGuid();
            await harness.Bus.Publish(new VoteCreatedIntegrationEvent(
                postId, System.Guid.NewGuid(), Direction: 1, UpvoteCount: 1, DownvoteCount: 0, Score: 1.0));

            (await harness.GetConsumerHarness<VoteConsumer>().Consumed.Any<VoteCreatedIntegrationEvent>())
                .Should().BeTrue();

            // The consumer keeps the Redis hot counter warm (the realtime VoteChanged push is owned by the
            // API handler — VoteConsumer has no IHubContext dependency, so it cannot double-push).
            await feedStore.Received(1).IncrementPostVotesAsync(
                postId, 1, 0, Arg.Any<CancellationToken>());
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task PostCreated_fans_out_notifications_to_followers_in_the_worker()
    {
        var topicFollower = System.Guid.NewGuid();
        var communityFollower = System.Guid.NewGuid();

        var read = Substitute.For<ICommunityReadService>();
        read.GetTopicFollowerIdsAsync(Arg.Any<System.Guid>(), Arg.Any<System.Guid?>(), Arg.Any<CancellationToken>())
            .Returns(new[] { topicFollower });
        read.GetCommunityFollowerIdsAsync(Arg.Any<System.Guid>(), Arg.Any<System.Guid?>(), Arg.Any<CancellationToken>())
            .Returns(new[] { communityFollower });

        var dispatcher = Substitute.For<INotificationMessageDispatcher>();

        await using var provider = new ServiceCollection()
            .AddLogging()
            .AddSingleton(read)
            .AddSingleton(dispatcher)
            .AddSingleton(Substitute.For<ICceDbContext>()) // injected but unused on the PostCreated path
            .AddMassTransitTestHarness(x => x.AddConsumer<NotificationConsumer>())
            .BuildServiceProvider(validateScopes: true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        try
        {
            await harness.Bus.Publish(new PostCreatedIntegrationEvent(
                PostId: System.Guid.NewGuid(),
                CommunityId: System.Guid.NewGuid(),
                TopicId: System.Guid.NewGuid(),
                AuthorId: System.Guid.NewGuid(),
                PublishedOn: System.DateTimeOffset.UtcNow,
                IsExpert: false,
                Locale: "ar"));

            (await harness.GetConsumerHarness<NotificationConsumer>().Consumed.Any<PostCreatedIntegrationEvent>())
                .Should().BeTrue();

            // One notification per distinct follower, carrying the event's locale (proves Locale round-trips).
            await dispatcher.Received(1).DispatchAsync(
                Arg.Is<NotificationMessage>(m => m.RecipientUserId == topicFollower && m.Locale == "ar"),
                Arg.Any<CancellationToken>());
            await dispatcher.Received(1).DispatchAsync(
                Arg.Is<NotificationMessage>(m => m.RecipientUserId == communityFollower && m.Locale == "ar"),
                Arg.Any<CancellationToken>());
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task PostCreated_pushes_newpost_to_community_and_topic_groups()
    {
        var proxy = Substitute.For<IClientProxy>();
        var clients = Substitute.For<IHubClients>();
        clients.Group(Arg.Any<string>()).Returns(proxy);
        var hub = Substitute.For<IHubContext<NotificationsHub>>();
        hub.Clients.Returns(clients);

        await using var provider = new ServiceCollection()
            .AddLogging()
            .AddSingleton(hub)
            .AddMassTransitTestHarness(x => x.AddConsumer<SignalRConsumer>())
            .BuildServiceProvider(validateScopes: true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        try
        {
            var communityId = System.Guid.NewGuid();
            var topicId = System.Guid.NewGuid();
            await harness.Bus.Publish(new PostCreatedIntegrationEvent(
                PostId: System.Guid.NewGuid(),
                CommunityId: communityId,
                TopicId: topicId,
                AuthorId: System.Guid.NewGuid(),
                PublishedOn: System.DateTimeOffset.UtcNow,
                IsExpert: false,
                Locale: "en"));

            (await harness.GetConsumerHarness<SignalRConsumer>().Consumed.Any<PostCreatedIntegrationEvent>())
                .Should().BeTrue();

            // NewPost pushed to both the community and topic groups (SendAsync → SendCoreAsync underneath).
            clients.Received(1).Group(RealtimeGroups.Community(communityId));
            clients.Received(1).Group(RealtimeGroups.Topic(topicId));
            await proxy.Received(2).SendCoreAsync(
                RealtimeEvents.NewPost, Arg.Any<object[]>(), Arg.Any<CancellationToken>());
        }
        finally
        {
            await harness.Stop();
        }
    }
}
