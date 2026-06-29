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
    CommunityPostReplied = 10,
    CommunityPostVoted = 11,
    CommunityJoinRequested = 12,
    CommunityJoinApproved = 13,
    CommunityPostDeleted = 14,
    TopicNewPost = 15,
    CommunityNewPost = 16,
    CommunityUserMentioned = 17,
    CommunityContentRejected = 18,
}
