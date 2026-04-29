using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Sanitization;
using CCE.Domain.Common;
using CCE.Domain.Community;
using MediatR;

namespace CCE.Application.Community.Commands.CreatePost;

public sealed class CreatePostCommandHandler : IRequestHandler<CreatePostCommand, Guid>
{
    private readonly ICommunityWriteService _service;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IHtmlSanitizer _sanitizer;
    private readonly ISystemClock _clock;

    public CreatePostCommandHandler(
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

    public async Task<Guid> Handle(CreatePostCommand request, CancellationToken cancellationToken)
    {
        var authorId = _currentUser.GetUserId()
            ?? throw new DomainException("Cannot create a post without a user identity.");

        var sanitized = _sanitizer.Sanitize(request.Content);
        var post = Post.Create(request.TopicId, authorId, sanitized, request.Locale, request.IsAnswerable, _clock);
        await _service.SavePostAsync(post, cancellationToken).ConfigureAwait(false);
        return post.Id;
    }
}
