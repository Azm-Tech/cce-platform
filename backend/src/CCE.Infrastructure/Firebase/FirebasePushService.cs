using CCE.Application.Notifications;
using FirebaseAdmin.Messaging;

namespace CCE.Infrastructure.Firebase;

public sealed class FirebasePushService : IFirebasePushService
{
    private readonly IFirebaseMessagingService _messaging;

    public FirebasePushService(IFirebaseMessagingService messaging)
    {
        _messaging = messaging;
    }

    public async Task<(int Sent, int Failed)> SendAsync(
        string token, string title, string body, CancellationToken ct)
    {
        var message = new MulticastMessage
        {
            Tokens = new[] { token },
            Notification = new Notification { Title = title, Body = body }
        };
        var response = await _messaging.SendMulticastAsync(message, ct).ConfigureAwait(false);
        return (response.SuccessCount, response.FailureCount);
    }
}
