using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Queries.ListHomepageSections;

public sealed class ListHomepageSectionsQueryHandler
    : IRequestHandler<ListHomepageSectionsQuery, Response<System.Collections.Generic.IReadOnlyList<HomepageSectionDto>>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public ListHomepageSectionsQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<System.Collections.Generic.IReadOnlyList<HomepageSectionDto>>> Handle(
        ListHomepageSectionsQuery request,
        CancellationToken cancellationToken)
    {
        var list = await _db.HomepageSections
            .OrderBy(s => s.OrderIndex)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);
        return _msg.Ok((System.Collections.Generic.IReadOnlyList<HomepageSectionDto>)list.Select(MapToDto).ToList(), MessageKeys.General.ITEMS_LISTED);
    }

    internal static HomepageSectionDto MapToDto(HomepageSection s) => new(
        s.Id, s.SectionType, s.OrderIndex, s.ContentAr, s.ContentEn, s.IsActive);
}
