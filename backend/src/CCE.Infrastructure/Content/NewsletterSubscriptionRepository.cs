using CCE.Application.Content;
using CCE.Domain.Content;
using CCE.Domain.Identity;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Content;

public sealed class NewsletterSubscriptionRepository
    : Repository<NewsletterSubscription, Guid>, INewsletterSubscriptionRepository
{
    public NewsletterSubscriptionRepository(CceDbContext db) : base(db) { }

    public Task<NewsletterSubscription?> FindByEmailAsync(string email, CancellationToken ct)
        => Db.NewsletterSubscriptions
            .FirstOrDefaultAsync(s => s.Email == email, ct);

    public async Task<IReadOnlyList<NewsletterAudienceMember>> GetAudienceAsync(
        Guid? excludeUserId, CancellationToken ct)
    {
        // Single LEFT JOIN query: newsletter_subscriptions LEFT JOIN asp_net_users.
        // Join condition on NormalizedEmail (already stored uppercase) == sub.Email —
        // SQL Server's default CI collation makes this case-insensitive at the DB level
        // with no client-side string conversion. Filter conditions stay on the join side
        // so unmatched subscribers still appear (newsletter-only, no account).
        var rows = await (
            from sub in Db.NewsletterSubscriptions.AsNoTracking()
            where sub.IsConfirmed && sub.UnsubscribedOn == null
            from u in Db.Users.AsNoTracking()
                .Where(u => !u.IsDeleted
                            && u.Status == UserStatus.Active
                            && u.NormalizedEmail == sub.Email)
                .DefaultIfEmpty()
            where !excludeUserId.HasValue || u == null || u.Id != excludeUserId.Value
            select new
            {
                sub.Email,
                UserLocale = (string?)u.LocalePreference,
                SubLocale = sub.LocalePreference,
                UserId = (Guid?)u.Id,
                FirstName = (string?)u.FirstName,
                LastName = (string?)u.LastName,
            }
        ).ToListAsync(ct).ConfigureAwait(false);

        return rows
            .Select(r => new NewsletterAudienceMember(
                r.Email,
                r.UserLocale ?? r.SubLocale,
                r.UserId,
                r.UserId.HasValue
                    ? $"{r.FirstName ?? string.Empty} {r.LastName ?? string.Empty}".Trim()
                    : string.Empty))
            .ToList();
    }
}
