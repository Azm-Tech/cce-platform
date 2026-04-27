using CCE.Domain.Common;
using CCE.Domain.Content;
using CCE.Domain.Content.Events;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Content;

public class NewsletterSubscriptionTests
{
    private static FakeSystemClock NewClock() => new();

    [Fact]
    public void Subscribe_creates_unconfirmed_record_with_token()
    {
        var clock = NewClock();
        var s = NewsletterSubscription.Subscribe("a@b.com", "ar", clock);

        s.Email.Should().Be("a@b.com");
        s.LocalePreference.Should().Be("ar");
        s.IsConfirmed.Should().BeFalse();
        s.ConfirmationToken.Should().NotBeNullOrWhiteSpace();
        s.ConfirmedOn.Should().BeNull();
    }

    [Fact]
    public void Subscribe_with_invalid_email_throws()
    {
        var clock = NewClock();
        var act = () => NewsletterSubscription.Subscribe("not-an-email", "ar", clock);
        act.Should().Throw<DomainException>().WithMessage("*email*");
    }

    [Fact]
    public void Subscribe_with_invalid_locale_throws()
    {
        var clock = NewClock();
        var act = () => NewsletterSubscription.Subscribe("a@b.com", "fr", clock);
        act.Should().Throw<DomainException>().WithMessage("*locale*");
    }

    [Fact]
    public void Confirm_with_correct_token_sets_IsConfirmed_and_raises_event()
    {
        var clock = NewClock();
        var s = NewsletterSubscription.Subscribe("a@b.com", "ar", clock);
        var token = s.ConfirmationToken;
        clock.Advance(System.TimeSpan.FromMinutes(2));

        s.Confirm(token, clock);

        s.IsConfirmed.Should().BeTrue();
        s.ConfirmedOn.Should().Be(clock.UtcNow);
        s.DomainEvents.OfType<NewsletterConfirmedEvent>().Should().HaveCount(1);
    }

    [Fact]
    public void Confirm_with_wrong_token_throws()
    {
        var clock = NewClock();
        var s = NewsletterSubscription.Subscribe("a@b.com", "ar", clock);

        var act = () => s.Confirm("wrong-token", clock);
        act.Should().Throw<DomainException>().WithMessage("*token*");
    }

    [Fact]
    public void Cannot_confirm_twice()
    {
        var clock = NewClock();
        var s = NewsletterSubscription.Subscribe("a@b.com", "ar", clock);
        var token = s.ConfirmationToken;
        s.Confirm(token, clock);

        var act = () => s.Confirm(token, clock);
        act.Should().Throw<DomainException>().WithMessage("*already*");
    }

    [Fact]
    public void Unsubscribe_after_confirm_records_unsubscribedOn()
    {
        var clock = NewClock();
        var s = NewsletterSubscription.Subscribe("a@b.com", "ar", clock);
        s.Confirm(s.ConfirmationToken, clock);
        clock.Advance(System.TimeSpan.FromDays(7));

        s.Unsubscribe(clock);

        s.UnsubscribedOn.Should().Be(clock.UtcNow);
    }
}
