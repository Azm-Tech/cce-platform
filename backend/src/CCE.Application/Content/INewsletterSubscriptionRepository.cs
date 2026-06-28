using CCE.Application.Common.Interfaces;
using CCE.Domain.Content;

namespace CCE.Application.Content;

public sealed record NewsletterAudienceMember(string Email, string Locale, System.Guid? UserId, string RecipientName);

public interface INewsletterSubscriptionRepository : IRepository<NewsletterSubscription, System.Guid>
{
    Task<CCE.Domain.Content.NewsletterSubscription?> FindByEmailAsync(string email, CancellationToken ct);

    /// <summary>
    /// Returns confirmed newsletter subscribers (not unsubscribed), enriched with registered-user
    /// data via a LEFT JOIN on normalised email. Matched active users contribute UserId + their
    /// LocalePreference; unmatched newsletter-only addresses get UserId = null and their
    /// subscription locale. <paramref name="excludeUserId"/> removes the content author (who is
    /// already notified by the in-process handler) from the broadcast set.
    /// </summary>
    Task<System.Collections.Generic.IReadOnlyList<NewsletterAudienceMember>> GetAudienceAsync(
        System.Guid? excludeUserId, CancellationToken ct);
}
