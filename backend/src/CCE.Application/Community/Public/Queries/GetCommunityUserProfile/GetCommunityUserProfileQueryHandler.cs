using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Community.Public.Dtos;
using CCE.Application.Errors;
using CCE.Application.Messages;
using CCE.Domain.Community;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CCE.Application.Community.Public.Queries.GetCommunityUserProfile;

/// <summary>US030 read path (§A.1): user info + expert badge + post/reply counts.</summary>
public sealed class GetCommunityUserProfileQueryHandler
    : IRequestHandler<GetCommunityUserProfileQuery, Response<CommunityUserProfileDto>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public GetCommunityUserProfileQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<CommunityUserProfileDto>> Handle(
        GetCommunityUserProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .Where(u => u.Id == request.UserId)
            .Select(u => new { u.Id, u.FirstName, u.LastName, u.JobTitle, u.OrganizationName, u.AvatarUrl, u.FollowerCount, u.FollowingCount })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (user is null) return _msg.UserNotFound<CommunityUserProfileDto>();

        var isExpert = await _db.ExpertProfiles.AnyAsync(e => e.UserId == request.UserId, cancellationToken).ConfigureAwait(false);
        var postCount = await _db.Posts
            .CountAsync(p => p.AuthorId == request.UserId && p.Status == PostStatus.Published, cancellationToken)
            .ConfigureAwait(false);
        var replyCount = await _db.PostReplies
            .CountAsync(r => r.AuthorId == request.UserId, cancellationToken)
            .ConfigureAwait(false);

        var dto = new CommunityUserProfileDto(
            user.Id, user.FirstName, user.LastName, user.JobTitle, user.OrganizationName,
            user.AvatarUrl, isExpert, postCount, replyCount, user.FollowerCount, user.FollowingCount);
        return _msg.Ok(dto, "ITEMS_LISTED");
    }
}
