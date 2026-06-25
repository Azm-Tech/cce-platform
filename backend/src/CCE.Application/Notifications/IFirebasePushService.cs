namespace CCE.Application.Notifications;

public interface IFirebasePushService
{
    Task<(int Sent, int Failed)> SendAsync(string token, string title, string body, CancellationToken ct);
}
