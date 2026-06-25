using System.Linq;
using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Sanitization;
using CCE.Application.Messages;
using CCE.Application.Identity;

using CCE.Domain.Common;
using CCE.Domain.Community;
using MediatR;

namespace CCE.Application.Community.Commands.CreatePost;

/// <summary>
/// US026 write path (§A.1): build a draft via the aggregate, attach tags, optionally publish,
/// then commit once via the context (UoW). Returns the new post id wrapped in <see cref="Response{T}"/>.
/// </summary>
public sealed class CreatePostCommandHandler
    : IRequestHandler<CreatePostCommand, Response<Guid>>
{
    private readonly IPostRepository _repo;
    private readonly ICommunityRepository _communityRepo;
    private readonly IPollRepository _pollRepo;
    private readonly ICommunityAccessGuard _accessGuard;
    private readonly ICceDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IHtmlSanitizer _sanitizer;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _msg;
    private readonly IUserRepository _userRepo;

    public CreatePostCommandHandler(
        IPostRepository repo,
        ICommunityRepository communityRepo,
        IPollRepository pollRepo,
        ICommunityAccessGuard accessGuard,
        ICceDbContext db,
        ICurrentUserAccessor currentUser,
        IHtmlSanitizer sanitizer,
        ISystemClock clock,
        MessageFactory msg,
        IUserRepository userRepo)
    {
        _repo = repo;
        _communityRepo = communityRepo;
        _pollRepo = pollRepo;
        _accessGuard = accessGuard;
        _db = db;
        _currentUser = currentUser;
        _sanitizer = sanitizer;
        _clock = clock;
        _msg = msg;
        _userRepo = userRepo;
    }

    public async Task<Response<Guid>> Handle(CreatePostCommand request, CancellationToken cancellationToken)
    {
        var authorId = _currentUser.GetUserId();
        if (authorId is null || authorId == Guid.Empty)
            return _msg.Unauthorized<Guid>(MessageKeys.Identity.NOT_AUTHENTICATED);

        if (!await _accessGuard.CanPostAsync(request.CommunityId, authorId.Value, cancellationToken).ConfigureAwait(false))
        {
            if (request.CommunityId == CommunitySeedIds.GeneralCommunityId)
            {
                var membership = CommunityMembership.Join(
                    request.CommunityId, authorId.Value, CommunityRole.Member, _clock);
                _communityRepo.AddMembership(membership);
                var community = await _communityRepo.GetAsync(
                    request.CommunityId, cancellationToken).ConfigureAwait(false);
                community?.IncrementMembers();
                await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return _msg.Forbidden<Guid>(MessageKeys.General.FORBIDDEN);
            }
        }

        if (!await _repo.TopicExistsAsync(request.TopicId, cancellationToken).ConfigureAwait(false))
            return _msg.NotFound<Guid>(MessageKeys.Community.TOPIC_NOT_FOUND);

        var sanitized = request.Content is null ? null : _sanitizer.Sanitize(request.Content);
        var post = Post.CreateDraft(request.CommunityId, request.TopicId, authorId.Value, request.Type,
            request.Title, sanitized, request.Locale, _clock);

        if (request.TagIds.Count > 0)
        {
            var tags = await _repo.GetTagsAsync(request.TagIds, cancellationToken).ConfigureAwait(false);
            post.SetTags(tags);
        }

        if (request.Attachments.Count > 0)
        {
            if (request.Attachments.Count > Post.MaxAttachments)
                return _msg.BusinessRule<Guid>(MessageKeys.Media.FILE_TOO_LARGE);

            var assetIds = request.Attachments.Select(a => a.AssetFileId).Distinct().ToList();
            var assets = (await _repo.GetAssetsAsync(assetIds, cancellationToken).ConfigureAwait(false))
                .ToDictionary(a => a.Id);

            foreach (var att in request.Attachments)
            {
                if (!assets.TryGetValue(att.AssetFileId, out var asset))
                    return _msg.NotFound<Guid>(MessageKeys.Content.ASSET_NOT_FOUND);
                if (asset.VirusScanStatus != Domain.Content.VirusScanStatus.Clean)
                    return _msg.BusinessRule<Guid>(MessageKeys.Content.ASSET_NOT_CLEAN);

                var allowed = att.Kind == Domain.Community.AttachmentKind.Media
                    ? PostAttachmentPolicy.MediaMimeTypes
                    : PostAttachmentPolicy.DocumentMimeTypes;
                if (!allowed.Contains(asset.MimeType))
                    return _msg.BusinessRule<Guid>(MessageKeys.Media.INVALID_FILE_TYPE);
                if (att.Kind == Domain.Community.AttachmentKind.Document
                    && asset.SizeBytes > PostAttachmentPolicy.MaxDocumentSizeBytes)
                    return _msg.BusinessRule<Guid>(MessageKeys.Media.FILE_TOO_LARGE);

                post.AddAttachment(att.AssetFileId, att.Kind, att.SortOrder, att.MetadataJson);
            }
        }

        if (request.Type == PostType.Poll)
        {
            if (request.Poll is null)
                return _msg.BusinessRule<Guid>(MessageKeys.Validation.REQUIRED_FIELD);
            var poll = Poll.Create(post.Id, request.Poll.Deadline, request.Poll.AllowMultiple,
                request.Poll.IsAnonymous, request.Poll.ShowResultsBeforeClose, request.Poll.OptionLabels, _clock);
            _pollRepo.AddPoll(poll);
        }

        if (!request.SaveAsDraft)
        {
            post.Publish(_clock);
            var community = await _communityRepo.GetAsync(request.CommunityId, cancellationToken).ConfigureAwait(false);
            community?.IncrementPosts();
            var author = await _userRepo.FindAsync(authorId.Value, cancellationToken).ConfigureAwait(false);
            author?.IncrementPostsCount();
        }

        _repo.Add(post);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Realtime fan-out for a published post (community + topic feeds) happens asynchronously in the
        // Worker: Post.Publish raises PostCreatedEvent → PostCreatedBusPublisher → SignalRConsumer. The
        // API stays publish-only here (no direct SignalR push).
        return _msg.Ok(post.Id, request.SaveAsDraft
            ? MessageKeys.Community.POST_DRAFT_SAVED
            : MessageKeys.Community.POST_CREATED);
    }
}
