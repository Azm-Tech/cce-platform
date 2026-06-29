namespace CCE.Application.Common.Messaging.IntegrationEvents;

/// <summary>
/// Published when a post or reply is created, triggering the async AI moderation consumer.
/// Carries the full content (not a snippet) so the AI provider analyses the whole text.
/// </summary>
public sealed record ContentModerationRequestedIntegrationEvent(
    System.Guid ContentId,
    string      ContentType,
    string      Content,
    string      Locale)
{
    /// <summary>Allowed values for <see cref="ContentType"/>.</summary>
    public static class ContentTypes
    {
        public const string Post  = "post";
        public const string Reply = "reply";
    }
}
