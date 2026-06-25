using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Community.Public.Dtos;
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
            .Select(u => new { u.Id, u.FirstName, u.LastName, u.JobTitle, u.OrganizationName, u.AvatarUrl, u.PostsCount, u.CommentsCount, u.FollowerCount, u.FollowingCount, u.CreatedOn, u.CountryId })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (user is null) return _msg.NotFound<CommunityUserProfileDto>(MessageKeys.Identity.USER_NOT_FOUND);

        var isExpert = await _db.ExpertProfiles.AnyAsync(e => e.UserId == request.UserId, cancellationToken).ConfigureAwait(false);

        string? expertBioAr = null;
        string? expertBioEn = null;
        if (isExpert)
        {
            var expert = await _db.ExpertProfiles.AsNoTracking()
                .Where(e => e.UserId == request.UserId)
                .Select(e => new { e.BioAr, e.BioEn })
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
            if (expert is not null)
            {
                expertBioAr = expert.BioAr;
                expertBioEn = expert.BioEn;
            }
        }

        string? countryNameAr = null;
        string? countryNameEn = null;
        if (user.CountryId.HasValue)
        {
            var country = await _db.Countries.AsNoTracking()
                .Where(c => c.Id == user.CountryId.Value)
                .Select(c => new { c.NameAr, c.NameEn })
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
            if (country is not null)
            {
                countryNameAr = country.NameAr;
                countryNameEn = country.NameEn;
            }
        }

        var isFollowed = request.CurrentUserId.HasValue
            && await _db.UserFollows.AsNoTracking()
                .AnyAsync(uf => uf.FollowerId == request.CurrentUserId.Value
                             && uf.FollowedId == request.UserId, cancellationToken)
                .ConfigureAwait(false);

        var dto = new CommunityUserProfileDto(
            user.Id, user.FirstName, user.LastName, user.JobTitle, user.OrganizationName,
            user.AvatarUrl, isExpert, user.PostsCount, user.CommentsCount, user.FollowerCount, user.FollowingCount,
            isFollowed, expertBioAr, expertBioEn, countryNameAr, countryNameEn, user.CreatedOn);
        return _msg.Ok(dto, MessageKeys.General.ITEMS_LISTED);
    }
}
