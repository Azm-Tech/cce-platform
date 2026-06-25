using FirebaseAdmin.Messaging;

namespace CCE.Infrastructure.Firebase;

public interface IFirebaseMessagingService
{
    Task<BatchResponse> SendMulticastAsync(
        MulticastMessage message, CancellationToken cancellationToken);
}
