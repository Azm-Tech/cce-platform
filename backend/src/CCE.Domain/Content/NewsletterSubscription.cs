using System.Text.RegularExpressions;
using CCE.Domain.Common;
using CCE.Domain.Content.Events;

namespace CCE.Domain.Content;

/// <summary>
/// Email-list subscription with double opt-in. Subscribing creates a record with a
/// fresh confirmation token; confirming consumes the token and marks the subscription
/// active. Unsubscribing keeps the row but stamps <see cref="UnsubscribedOn"/>.
/// </summary>
[Audited]
public sealed class NewsletterSubscription : Entity<System.Guid>
{
    private static readonly Regex EmailPattern = new(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.Compiled);

    private NewsletterSubscription(
        System.Guid id,
        string email,
        string localePreference,
        string confirmationToken) : base(id)
    {
        Email = email;
        LocalePreference = localePreference;
        ConfirmationToken = confirmationToken;
    }

    public string Email { get; private set; }
    public string LocalePreference { get; private set; }
    public bool IsConfirmed { get; private set; }
    public string ConfirmationToken { get; private set; }
    public System.DateTimeOffset? ConfirmedOn { get; private set; }
    public System.DateTimeOffset? UnsubscribedOn { get; private set; }

    public static NewsletterSubscription Subscribe(string email, string localePreference, ISystemClock clock)
    {
        _ = clock;
        if (string.IsNullOrWhiteSpace(email) || !EmailPattern.IsMatch(email))
        {
            throw new DomainException($"email '{email}' is invalid.");
        }
        if (localePreference != "ar" && localePreference != "en")
        {
            throw new DomainException("locale must be 'ar' or 'en'.");
        }
        return new NewsletterSubscription(
            id: System.Guid.NewGuid(),
            email: email,
            localePreference: localePreference,
            confirmationToken: System.Guid.NewGuid().ToString("N"));
    }

    public void Confirm(string token, ISystemClock clock)
    {
        if (IsConfirmed)
        {
            throw new DomainException("Subscription already confirmed.");
        }
        if (string.IsNullOrWhiteSpace(token) || token != ConfirmationToken)
        {
            throw new DomainException("Invalid confirmation token.");
        }
        IsConfirmed = true;
        ConfirmedOn = clock.UtcNow;
        RaiseDomainEvent(new NewsletterConfirmedEvent(Id, Email, ConfirmedOn.Value));
    }

    public void Unsubscribe(ISystemClock clock)
    {
        UnsubscribedOn = clock.UtcNow;
    }
}
