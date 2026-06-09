namespace CCE.Application.Common.Realtime;

/// <summary>
/// Client-facing SignalR event (method) names. Stable wire contract — the frontend listens for these
/// exact names, so do not rename (deprecate + add instead).
/// </summary>
public static class RealtimeEvents
{
    // Existing (kept verbatim)
    public const string ReceiveNotification = "ReceiveNotification";
    public const string NewReply = "NewReply";
    public const string VoteChanged = "VoteChanged";
    public const string PollResultsChanged = "PollResultsChanged";

    // New
    public const string NewPost = "NewPost";
    public const string PostModerated = "PostModerated";
    public const string ContentModerated = "ContentModerated";
    public const string PresenceChanged = "PresenceChanged";
    public const string TypingChanged = "TypingChanged";
}
