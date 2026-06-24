using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;

using CCE.Domain.Common;
using CCE.Domain.Community;
using MediatR;

namespace CCE.Application.Community.Commands.CreateCommunity;

public sealed class CreateCommunityCommandHandler
    : IRequestHandler<CreateCommunityCommand, Response<Guid>>
{
    private readonly ICommunityRepository _repo;
    private readonly ICceDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _msg;

    public CreateCommunityCommandHandler(
        ICommunityRepository repo, ICceDbContext db, ICurrentUserAccessor currentUser,
        ISystemClock clock, MessageFactory msg)
    {
        _repo = repo;
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _msg = msg;
    }

    public async Task<Response<Guid>> Handle(CreateCommunityCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId();
        if (userId is null || userId == Guid.Empty) return _msg.NotAuthenticated<Guid>();

        if (await _repo.SlugExistsAsync(request.Slug, cancellationToken).ConfigureAwait(false))
            return _msg.Conflict<Guid>(MessageKeys.General.DUPLICATE_VALUE);

        var community = Domain.Community.Community.Create(
            request.NameAr, request.NameEn, request.DescriptionAr, request.DescriptionEn,
            request.Slug, request.Visibility, request.PresentationJson);

        _repo.AddCommunity(community);
        _repo.AddMembership(CommunityMembership.Join(community.Id, userId.Value, CommunityRole.Moderator, _clock));
        community.IncrementMembers();

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return _msg.Ok(community.Id, MessageKeys.General.SUCCESS_CREATED);
    }
}
