namespace CCE.Domain.Notifications;

public enum NotificationEventType
{
    ExpertRequestApproved = 0,
    ExpertRequestRejected = 1,
    CountryResourceApproved = 2,
    CountryResourceRejected = 3,
    NewsPublished = 4,
    ResourcePublished = 5,
    EventScheduled = 6,
    CommunityPostCreated = 7,
    AdminAccountCreated = 8,
    CountryContentSubmitted = 9,
}
