using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Commands.SubscribeNewsletter;

public sealed class SubscribeNewsletterCommandHandler
    : IRequestHandler<SubscribeNewsletterCommand, Response<VoidData>>
{
    private readonly INewsletterSubscriptionRepository _repo;
    private readonly ICceDbContext _db;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _messages;

    public SubscribeNewsletterCommandHandler(
        INewsletterSubscriptionRepository repo,
        ICceDbContext db,
        ISystemClock clock,
        MessageFactory messages)
    {
        _repo = repo;
        _db = db;
        _clock = clock;
        _messages = messages;
    }

    public async Task<Response<VoidData>> Handle(
        SubscribeNewsletterCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repo.FindByEmailAsync(request.Email, cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            if (existing.UnsubscribedOn is null)
                return _messages.Ok(MessageKeys.Content.NEWSLETTER_SUBSCRIBED);

            existing.Resubscribe(request.Locale, _clock);
        }
        else
        {
            var subscription = NewsletterSubscription.Subscribe(request.Email, request.Locale, _clock);
            await _repo.AddAsync(subscription, cancellationToken).ConfigureAwait(false);
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return _messages.Ok(MessageKeys.Content.NEWSLETTER_SUBSCRIBED);
    }
}
