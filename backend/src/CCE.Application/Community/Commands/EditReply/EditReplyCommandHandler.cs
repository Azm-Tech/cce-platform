using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Messaging;
using CCE.Application.Common.Messaging.IntegrationEvents;
using CCE.Application.Common.Sanitization;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Community;
using MediatR;

namespace CCE.Application.Community.Commands.EditReply;

public sealed class EditReplyCommandHandler : IRequestHandler<EditReplyCommand, Response<VoidData>>
{
    private static readonly TimeSpan EditWindow = TimeSpan.FromMinutes(15);

    private readonly ICommunityWriteService _service;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IHtmlSanitizer _sanitizer;
    private readonly IIntegrationEventPublisher _publisher;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _msg;

    public EditReplyCommandHandler(
        ICommunityWriteService service,
        ICurrentUserAccessor currentUser,
        IHtmlSanitizer sanitizer,
        IIntegrationEventPublisher publisher,
        ISystemClock clock,
        MessageFactory msg)
    {
        _service = service;
        _currentUser = currentUser;
        _sanitizer = sanitizer;
        _publisher = publisher;
        _clock = clock;
        _msg = msg;
    }

    public async Task<Response<VoidData>> Handle(EditReplyCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId()
            ?? throw new DomainException("Cannot edit a reply without a user identity.");

        var reply = await _service.FindReplyAsync(request.ReplyId, cancellationToken).ConfigureAwait(false);
        if (reply is null)
        {
            throw new KeyNotFoundException($"Reply {request.ReplyId} not found.");
        }

        if (reply.AuthorId != userId)
        {
            throw new UnauthorizedAccessException("Only the reply author may edit this reply.");
        }

        var elapsed = _clock.UtcNow - reply.CreatedOn;
        if (elapsed > EditWindow)
        {
            throw new DomainException($"Replies can only be edited within {(int)EditWindow.TotalMinutes} minutes of creation.");
        }

        var sanitized = _sanitizer.Sanitize(request.Content);
        reply.EditContent(sanitized, userId, _clock);

        // Edited content must be re-moderated: reset to Pending (so the consumer's idempotency guard
        // re-analyses it) and re-queue the moderation request via the outbox — atomic with the save
        // below. Otherwise a user could edit approved content into a violation undetected.
        reply.SetModerationStatus(ModerationStatus.Pending);
        await _publisher.PublishAsync(new ContentModerationRequestedIntegrationEvent(
            reply.Id,
            ContentModerationRequestedIntegrationEvent.ContentTypes.Reply,
            sanitized,
            reply.Locale), cancellationToken).ConfigureAwait(false);

        await _service.UpdateReplyAsync(reply, cancellationToken).ConfigureAwait(false);
        return _msg.Ok(MessageKeys.General.SUCCESS_OPERATION);
    }
}
