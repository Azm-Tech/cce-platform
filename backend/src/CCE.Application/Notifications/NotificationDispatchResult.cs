using CCE.Domain.Notifications;

namespace CCE.Application.Notifications;

public sealed record NotificationDispatchResult(
    string TemplateCode,
    Guid? RecipientUserId,
    IReadOnlyCollection<NotificationChannelDispatchResult> Results)
{
    public bool IsSuccess => Results.All(r => r.Status != NotificationDeliveryStatus.Failed);
}
