using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Community.Public.Dtos;
using CCE.Application.Errors;
using CCE.Application.Messages;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CCE.Application.Community.Public.Queries.GetCommunityBySlug;

public sealed class GetCommunityBySlugQueryHandler
    : IRequestHandler<GetCommunityBySlugQuery, Response<CommunityDto>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public GetCommunityBySlugQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<CommunityDto>> Handle(
        GetCommunityBySlugQuery request, CancellationToken cancellationToken)
    {
        var dto = await _db.Communities
            .Where(c => c.Slug == request.Slug && c.IsActive)
            .Select(c => new CommunityDto(
                c.Id, c.NameAr, c.NameEn, c.DescriptionAr, c.DescriptionEn,
                c.Slug, c.Visibility, c.MemberCount, c.PresentationJson))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return dto is null
            ? _msg.NotFound<CommunityDto>(ApplicationErrors.Community.COMMUNITY_NOT_FOUND)
            : _msg.Ok(dto, "ITEMS_LISTED");
    }
}
