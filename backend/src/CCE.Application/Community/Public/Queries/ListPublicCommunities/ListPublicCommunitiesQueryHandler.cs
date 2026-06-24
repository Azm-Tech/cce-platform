using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Community.Public.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Community;
using MediatR;

namespace CCE.Application.Community.Public.Queries.ListPublicCommunities;

public sealed class ListPublicCommunitiesQueryHandler
    : IRequestHandler<ListPublicCommunitiesQuery, Response<PagedResult<CommunityDto>>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public ListPublicCommunitiesQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<PagedResult<CommunityDto>>> Handle(
        ListPublicCommunitiesQuery request, CancellationToken cancellationToken)
    {
        var paged = await _db.Communities
            .Where(c => c.IsActive && c.Visibility == CommunityVisibility.Public)
            .OrderByDescending(c => c.MemberCount)
            .Select(c => new CommunityDto(
                c.Id, c.NameAr, c.NameEn, c.DescriptionAr, c.DescriptionEn,
                c.Slug, c.Visibility, c.MemberCount, c.PresentationJson))
            .ToPagedResultAsync(request.Page, request.PageSize, cancellationToken)
            .ConfigureAwait(false);

        return _msg.Ok(paged, MessageKeys.General.ITEMS_LISTED);
    }
}
