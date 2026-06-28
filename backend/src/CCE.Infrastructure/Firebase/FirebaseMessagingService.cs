using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CCE.Infrastructure.Firebase;

public sealed class FirebaseMessagingService : IFirebaseMessagingService
{
    private readonly FirebaseMessaging _messaging;
    private readonly ILogger<FirebaseMessagingService> _logger;

    public FirebaseMessagingService(
        IOptions<FirebaseOptions> options,
        ILogger<FirebaseMessagingService> logger)
    {
        _logger = logger;
        var opts = options.Value;

        // FirebaseApp is a process-wide singleton; DefaultInstance is null on first init.
        var app = FirebaseApp.DefaultInstance ?? FirebaseApp.Create(new AppOptions
        {
            Credential = GoogleCredential.FromJson(opts.ServiceAccountJson),
            ProjectId = opts.ProjectId
        });

        _messaging = FirebaseMessaging.GetMessaging(app);
    }

    public async Task<BatchResponse> SendMulticastAsync(
        MulticastMessage message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var response = await _messaging.SendEachForMulticastAsync(message, cancellationToken).ConfigureAwait(false);
        _logger.LogDebug(
            "FCM multicast: {SuccessCount} sent, {FailureCount} failed.",
            response.SuccessCount, response.FailureCount);
        return response;
    }
}
