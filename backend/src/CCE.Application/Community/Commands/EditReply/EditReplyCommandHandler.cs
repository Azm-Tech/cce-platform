using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Sanitization;
using CCE.Domain.Common;
using MediatR;

namespace CCE.Application.Community.Commands.EditReply;

public sealed class EditReplyCommandHandler : IRequestHandler<EditReplyCommand, Unit>
{
    private static readonly TimeSpan EditWindow = TimeSpan.FromMinutes(15);

    private readonly ICommunityWriteService _service;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IHtmlSanitizer _sanitizer;
    private readonly ISystemClock _clock;

    public EditReplyCommandHandler(
        ICommunityWriteService service,
        ICurrentUserAccessor currentUser,
        IHtmlSanitizer sanitizer,
        ISystemClock clock)
    {
        _service = service;
        _currentUser = currentUser;
        _sanitizer = sanitizer;
        _clock = clock;
    }

    public async Task<Unit> Handle(EditReplyCommand request, CancellationToken cancellationToken)
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
        reply.EditContent(sanitized);
        await _service.UpdateReplyAsync(reply, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
