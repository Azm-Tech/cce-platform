using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.CommunityLaws.Dtos;
using CCE.Application.Messages;
using CCE.Domain.CommunityLaws;
using MediatR;

namespace CCE.Application.CommunityLaws.Queries.GetCommunityLaws;

internal sealed class GetCommunityLawsQueryHandler(
    ICceDbContext _db,
    MessageFactory _msg)
    : IRequestHandler<GetCommunityLawsQuery, Response<List<CommunityLawSectionDto>>>
{
    public async Task<Response<List<CommunityLawSectionDto>>> Handle(
        GetCommunityLawsQuery q, CancellationToken ct)
    {
        var items = await _db.CommunityLawSections
            .OrderBy(e => e.OrderIndex)
            .Select(e => new CommunityLawSectionDto(
                e.Id,
                e.Title.Ar,
                e.Title.En,
                e.Content.Ar,
                e.Content.En,
                e.OrderIndex))
            .ToListAsyncEither(ct);

        return _msg.Ok(items, MessageKeys.General.ITEMS_LISTED);
    }
}
