using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Sanitization;
using CCE.Domain.Common;
using CCE.Domain.Community;
using MediatR;

namespace CCE.Application.Community.Commands.CreateReply;

public sealed class CreateReplyCommandHandler : IRequestHandler<CreateReplyCommand, Guid>
{
    private readonly ICommunityWriteService _service;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IHtmlSanitizer _sanitizer;
    private readonly ISystemClock _clock;

    public CreateReplyCommandHandler(
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

    public async Task<Guid> Handle(CreateReplyCommand request, CancellationToken cancellationToken)
    {
        var authorId = _currentUser.GetUserId()
            ?? throw new DomainException("Cannot create a reply without a user identity.");

        // Verify post exists
        var post = await _service.FindPostAsync(request.PostId, cancellationToken).ConfigureAwait(false);
        if (post is null)
        {
            throw new KeyNotFoundException($"Post {request.PostId} not found.");
        }

        var sanitized = _sanitizer.Sanitize(request.Content);
        // isByExpert = false for v0.1.0; role-check to be wired later
        var reply = PostReply.Create(request.PostId, authorId, sanitized, request.Locale, request.ParentReplyId, isByExpert: false, _clock);
        await _service.SaveReplyAsync(reply, cancellationToken).ConfigureAwait(false);
        return reply.Id;
    }
}
